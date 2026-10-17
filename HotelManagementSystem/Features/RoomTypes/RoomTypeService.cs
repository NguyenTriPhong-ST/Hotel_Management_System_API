using System.Text.Json;
using System.Text.RegularExpressions;
using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Features.RoomTypes;

public enum RoomTypeErrorKind { Invalid, NotFound, Conflict }

public sealed class RoomTypeException : Exception
{
    public RoomTypeErrorKind Kind { get; }

    public RoomTypeException(RoomTypeErrorKind kind, string message) : base(message)
        => Kind = kind;
}

public sealed class RoomTypeService
{
    private readonly AppDbContext _db;

    public RoomTypeService(AppDbContext db) => _db = db;

    /// <summary>
    /// UC-F04-01 - Tạo loại phòng. Kiểm tra mã duy nhất, sức chứa và giá trước khi lưu.
    /// </summary>
    public async Task<RoomTypeDto> CreateAsync(CreateRoomTypeRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        ValidateValues(request.Capacity, request.BasePricePerNight);

        if (await _db.RoomTypes.AnyAsync(x => x.Code == code))
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

        // Tạo bản ghi và nhật ký trong cùng một transaction.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.RoomTypes.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
            WriteAudit("Create", entity, actorId, actorName, correlationId,
                oldValues: null, newValues: Snapshot(entity));
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new RoomTypeException(RoomTypeErrorKind.Conflict, "Mã loại phòng đã tồn tại.");
        }

        return Map(entity);
    }

    /// <summary>
    /// UC-F04-02 và UC-F04-06 - Phân trang, lọc và sắp xếp loại phòng chưa lưu trữ.
    /// </summary>
    public async Task<RoomTypePagedDto> GetListAsync(RoomTypeListQuery request)
    {
        if (request.MinPrice > request.MaxPrice)
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "MinPrice không được lớn hơn MaxPrice.");

        if (((long)request.Page - 1) * request.PageSize > int.MaxValue)
            throw new RoomTypeException(RoomTypeErrorKind.Invalid, "Page quá lớn.");

        var sortBy = (request.SortBy ?? "name").Trim().ToLowerInvariant();
        var direction = (request.SortDirection ?? "asc").Trim().ToLowerInvariant();
        if (sortBy is not ("name" or "price"))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "SortBy chỉ nhận 'name' hoặc 'price'.");
        if (direction is not ("asc" or "desc"))
            throw new RoomTypeException(RoomTypeErrorKind.Invalid,
                "SortDirection chỉ nhận 'asc' hoặc 'desc'.");

        IQueryable<RoomType> query = _db.RoomTypes.AsNoTracking()
            .Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var keyword = request.Name.Trim();
            query = query.Where(x => x.Name.Contains(keyword));
        }
        if (request.MinCapacity.HasValue)
            query = query.Where(x => x.Capacity >= request.MinCapacity.Value);
        if (request.MinPrice.HasValue)
            query = query.Where(x => x.BasePricePerNight >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            query = query.Where(x => x.BasePricePerNight <= request.MaxPrice.Value);

        var count = await query.CountAsync();
        query = (sortBy, direction) switch
        {
            ("price", "asc") => query.OrderBy(x => x.BasePricePerNight).ThenBy(x => x.Id),
            ("price", "desc") => query.OrderByDescending(x => x.BasePricePerNight).ThenBy(x => x.Id),
            ("name", "desc") => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };

        var entities = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync();
        return new RoomTypePagedDto(entities.Select(Map).ToList(),
            request.Page, request.PageSize, count);
    }

    /// <summary>
    /// UC-F04-03 - Xem chi tiết loại phòng chưa lưu trữ theo ID.
    /// </summary>
    public async Task<RoomTypeDto> GetByIdAsync(int id)
    {
        var entity = await _db.RoomTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        return entity is null
            ? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.")
            : Map(entity);
    }

    /// <summary>
    /// UC-F04-04 - Cập nhật loại phòng; RowVersion chống ghi đè dữ liệu mới hơn.
    /// </summary>
    public async Task<RoomTypeDto> UpdateAsync(int id, UpdateRoomTypeRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        ValidateValues(request.Capacity, request.BasePricePerNight);
        var version = ParseRowVersion(request.RowVersion);

        var entity = await _db.RoomTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.");
        if (!entity.RowVersion.SequenceEqual(version))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "RowVersion đã cũ. Hãy GET lại chi tiết loại phòng.");
        if (await _db.RoomTypes.AnyAsync(x => x.Id != id && x.Code == code))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict, "Mã loại phòng đã tồn tại.");

        var oldValues = Snapshot(entity);
        _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = version;
        entity.Code = code;
        entity.Name = name;
        entity.Description = NormalizeDescription(request.Description);
        entity.Capacity = request.Capacity;
        entity.BasePricePerNight = request.BasePricePerNight;

        WriteAudit("Update", entity, actorId, actorName, correlationId,
            oldValues, Snapshot(entity));
        try
        {
            await _db.SaveChangesAsync();
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
        return Map(entity);
    }

    /// <summary>
    /// UC-F04-05 - Lưu trữ mềm loại phòng khi không còn phòng đang hoạt động tham chiếu.
    /// </summary>
    public async Task DeleteAsync(int id, int? actorId, string? actorName, string? correlationId)
    {
        var entity = await _db.RoomTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw new RoomTypeException(RoomTypeErrorKind.NotFound, "Không tìm thấy loại phòng.");

        if (await _db.Rooms.AnyAsync(x => x.RoomTypeId == id && x.DeletedAt == null))
            throw new RoomTypeException(RoomTypeErrorKind.Conflict,
                "Không thể lưu trữ loại phòng vì vẫn còn phòng đang sử dụng.");

        var oldValues = Snapshot(entity);
        entity.DeletedAt = DateTime.UtcNow;
        WriteAudit("SoftDelete", entity, actorId, actorName, correlationId,
            oldValues, Snapshot(entity));
        try
        {
            await _db.SaveChangesAsync();
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

    private static RoomTypeDto Map(RoomType x) => new(
        x.Id, x.Code, x.Name, x.Description, x.Capacity,
        x.BasePricePerNight, x.CreatedAt, x.UpdatedAt,
        Convert.ToBase64String(x.RowVersion));

    private static object Snapshot(RoomType x) => new
    {
        x.Id,
        x.Code,
        x.Name,
        x.Description,
        x.Capacity,
        x.BasePricePerNight,
        x.DeletedAt
    };

    private void WriteAudit(string action, RoomType entity,
        int? actorId, string? actorName, string? correlationId,
        object? oldValues, object? newValues)
    {
        _db.AuditLogs.Add(new AuditLog
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
        });
    }
}
