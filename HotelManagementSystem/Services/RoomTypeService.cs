using System.Text.Json;
using System.Text.RegularExpressions;
using HotelManagementSystem.DTOs.RoomTypes;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services;

public sealed class RoomTypeService
{
    private readonly IRoomTypeRepository _repository;

    public RoomTypeService(IRoomTypeRepository repository) => _repository = repository;

    // UC-F04-01 - Create
    public async Task<RoomTypeDto> CreateAsync(CreateRoomTypeRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        ValidateValues(request.Capacity, request.BasePricePerNight);
        if (await _repository.CodeExistsAsync(code))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "Mã loại phòng đã tồn tại, kể cả trong dữ liệu lưu trữ.");

        var entity = new RoomType
        {
            Code = code,
            Name = name,
            Description = NormalizeDescription(request.Description),
            Capacity = request.Capacity,
            BasePricePerNight = request.BasePricePerNight
        };
        try
        {
            await _repository.InsertWithAuditAsync(entity,
                saved => BuildAudit("Create", saved, actorId, actorName, correlationId,
                    oldValues: null, newValues: RoomTypeMapper.ToAuditSnapshot(saved)));
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new RoomTypeException(RoomTypeErrorKind.Conflict, "Mã loại phòng đã tồn tại.");
        }
        return RoomTypeMapper.ToDto(entity);
    }

    // UC-F04-02 & UC-F04-06 - Get list/search/filter/sort
    public async Task<RoomTypePagedDto> GetListAsync(RoomTypeListQuery request)
    {
        if (request.MinPrice > request.MaxPrice)
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "MinPrice không được lớn hơn MaxPrice.");
        if (request.Page <= 0 || request.PageSize is < 1 or > 100 ||
            ((long)request.Page - 1) * request.PageSize > int.MaxValue)
            throw new RoomTypeException(RoomTypeErrorKind.Invalid, "Page hoặc PageSize không hợp lệ.");

        var sortBy = (request.SortBy ?? "name").Trim().ToLowerInvariant();
        var direction = (request.SortDirection ?? "asc").Trim().ToLowerInvariant();
        if (sortBy is not ("name" or "price"))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "SortBy chỉ nhận 'name' hoặc 'price'.");
        if (direction is not ("asc" or "desc"))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "SortDirection chỉ nhận 'asc' hoặc 'desc'.");

        var options = new RoomTypeSearchOptions(request.Name, request.MinCapacity,
            request.MinPrice, request.MaxPrice,
            request.Page, request.PageSize, sortBy, direction);
        var (items, count) = await _repository.GetPagedAsync(options);
        return new RoomTypePagedDto(items.Select(RoomTypeMapper.ToDto).ToList(),
            request.Page, request.PageSize, count);
    }

    // UC-F04-03 - Get by ID
    public async Task<RoomTypeDto> GetByIdAsync(int id)
    {
        var entity = await _repository.FindActiveAsync(id);
        return entity is null
            ? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.")
            : RoomTypeMapper.ToDto(entity);
    }

    // UC-F04-04 - Update
    public async Task<RoomTypeDto> UpdateAsync(int id, UpdateRoomTypeRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        ValidateValues(request.Capacity, request.BasePricePerNight);
        var version = ParseRowVersion(request.RowVersion);
        var entity = await _repository.FindActiveAsync(id, tracking: true)
            ?? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.");
        if (!entity.RowVersion.SequenceEqual(version))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "RowVersion đã cũ. Hãy GET lại chi tiết loại phòng.");
        if (await _repository.CodeExistsAsync(code, excludingId: id))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict, "Mã loại phòng đã tồn tại.");

        var oldValues = RoomTypeMapper.ToAuditSnapshot(entity);
        entity.Code = code;
        entity.Name = name;
        entity.Description = NormalizeDescription(request.Description);
        entity.Capacity = request.Capacity;
        entity.BasePricePerNight = request.BasePricePerNight;
        var audit = BuildAudit("Update", entity, actorId, actorName, correlationId,
            oldValues, RoomTypeMapper.ToAuditSnapshot(entity));
        try
        {
            await _repository.SaveWithAuditAsync(entity, version, audit);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "Dữ liệu đã thay đổi. Hãy GET lại chi tiết để lấy RowVersion mới.");
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new RoomTypeException(RoomTypeErrorKind.Conflict, "Mã loại phòng đã tồn tại.");
        }
        return RoomTypeMapper.ToDto(entity);
    }

    // UC-F04-05 - Soft delete
    public async Task DeleteAsync(int id, int? actorId, string? actorName, string? correlationId)
    {
        var entity = await _repository.FindActiveAsync(id, tracking: true)
            ?? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.");
        if (await _repository.HasActiveRoomsAsync(id))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "Không thể lưu trữ loại phòng vì vẫn còn phòng đang sử dụng.");

        var originalVersion = (byte[])entity.RowVersion.Clone();
        var oldValues = RoomTypeMapper.ToAuditSnapshot(entity);
        entity.DeletedAt = DateTime.UtcNow;
        var audit = BuildAudit("SoftDelete", entity, actorId, actorName, correlationId,
            oldValues, RoomTypeMapper.ToAuditSnapshot(entity));
        try
        {
            await _repository.SaveWithAuditAsync(entity, originalVersion, audit);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "Loại phòng đã được thay đổi bởi yêu cầu khác.");
        }
    }

    private static string NormalizeCode(string? value)
    {
        var code = (value ?? "").Trim().ToUpperInvariant();
        if (!Regex.IsMatch(code, "^[A-Z0-9_-]{1,20}$"))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "Code phải gồm 1-20 ký tự A-Z, 0-9, dấu gạch dưới hoặc gạch ngang.");
        return code;
    }

    private static string NormalizeName(string? value)
    {
        var name = (value ?? "").Trim();
        if (name.Length is < 1 or > 100)
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "Name phải có từ 1 đến 100 ký tự.");
        return name;
    }

    private static string? NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateValues(int capacity, decimal price)
    {
        if (capacity <= 0 || price < 0 || price != decimal.Round(price, 2))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "Capacity phải > 0; giá phải >= 0 và có tối đa 2 chữ số thập phân.");
    }

    private static byte[] ParseRowVersion(string? value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value ?? "");
            if (bytes.Length == 8) return bytes;
        }
        catch (FormatException) { }
        throw new RoomTypeException(RoomTypeErrorKind.Invalid,
            "RowVersion phải là chuỗi Base64 hợp lệ (8 bytes). Lấy từ API chi tiết.");
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;

    private static AuditLog BuildAudit(string action, RoomType entity,
        int? actorId, string? actorName, string? correlationId,
        object? oldValues, object? newValues)
        => new()
        {
            ActorAccountId = actorId,
            ActorUsernameSnapshot = actorName?.Length > 50 ? actorName[..50] : actorName,
            Action = action,
            EntityType = "RoomType",
            EntityId = entity.Id.ToString(),
            EntityCodeSnapshot = entity.Code,
            OccurredAt = DateTime.UtcNow,
            Summary = $"{action} room type {entity.Code}",
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CorrelationId = correlationId
        };
}
