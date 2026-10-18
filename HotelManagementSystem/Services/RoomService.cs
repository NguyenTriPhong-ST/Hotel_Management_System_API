using System.Text.Json;
using System.Text.RegularExpressions;
using HotelManagementSystem.DTOs.Rooms;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services;

public sealed class RoomService
{
    private readonly IRoomRepository _repository;

    public RoomService(IRoomRepository repository) => _repository = repository;

    /// <summary>UC-F03-01 - Tạo phòng, kiểm tra mã, loại phòng, trạng thái.</summary>
    public async Task<RoomDto> CreateAsync(CreateRoomRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.RoomCode);
        ValidateStatuses(request.OperationalStatus, request.HousekeepingStatus);
        await EnsureRoomTypeExistsAsync(request.RoomTypeId);

        if (await _repository.CodeExistsAsync(code))
            throw new RoomException(RoomErrorKind.Conflict,
                "Mã phòng đã tồn tại, kể cả trong dữ liệu lưu trữ.");

        var room = new Room
        {
            RoomCode = code,
            RoomTypeId = request.RoomTypeId,
            FloorNumber = request.FloorNumber,
            Description = NormalizeDescription(request.Description),
            OperationalStatus = (RoomOperationalStatus)request.OperationalStatus,
            HousekeepingStatus = (HousekeepingStatus)request.HousekeepingStatus
        };

        try
        {
            await _repository.InsertWithAuditAsync(room,
                saved => BuildAudit("Create", saved, actorId, actorName, correlationId,
                    oldValues: null, newValues: RoomMapper.ToAuditSnapshot(saved)));
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new RoomException(RoomErrorKind.Conflict, "Mã phòng đã tồn tại.");
        }
        catch (DbUpdateException ex) when (IsForeignKeyError(ex))
        {
            throw new RoomException(RoomErrorKind.Conflict,
                "Loại phòng đã bị thay đổi hoặc không còn tồn tại.");
        }

        return await GetByIdAsync(room.Id);
    }

    /// <summary>UC-F03-02, UC-F03-07 - Danh sách, lọc, tìm kiếm, phân trang, sắp xếp.</summary>
    public async Task<RoomPagedDto> GetListAsync(RoomListQuery request)
    {
        var sortBy = (request.SortBy ?? "code").Trim().ToLowerInvariant();
        var direction = (request.SortDirection ?? "asc").Trim().ToLowerInvariant();
        if (sortBy is not ("code" or "floor"))
            throw new RoomException(RoomErrorKind.Invalid,
                "SortBy chỉ nhận 'code' hoặc 'floor'.");
        if (direction is not ("asc" or "desc"))
            throw new RoomException(RoomErrorKind.Invalid,
                "SortDirection chỉ nhận 'asc' hoặc 'desc'.");
        if (request.Page <= 0 || request.PageSize is < 1 or > 100 ||
            ((long)request.Page - 1) * request.PageSize > int.MaxValue)
            throw new RoomException(RoomErrorKind.Invalid,
                "Page hoặc PageSize không hợp lệ.");

        var options = new RoomSearchOptions(request.RoomCode, request.RoomTypeId,
            request.FloorNumber, request.OperationalStatus, request.HousekeepingStatus,
            request.IsOccupied, request.IsBlocked, request.Page, request.PageSize,
            sortBy, direction, DateTime.UtcNow);

        var (items, count) = await _repository.GetPagedAsync(options);
        return new RoomPagedDto(items.Select(RoomMapper.ToDto).ToList(),
            request.Page, request.PageSize, count);
    }

    /// <summary>UC-F03-03 - Lấy chi tiết phòng theo ID.</summary>
    public async Task<RoomDto> GetByIdAsync(int id)
    {
        var data = await _repository.GetDetailAsync(id, DateTime.UtcNow);
        return data is null
            ? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.")
            : RoomMapper.ToDto(data);
    }

    /// <summary>UC-F03-04 - Sửa thông tin phòng, không sửa trạng thái.</summary>
    public async Task<RoomDto> UpdateAsync(int id, UpdateRoomRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.RoomCode);
        var version = ParseRowVersion(request.RowVersion);
        await EnsureRoomTypeExistsAsync(request.RoomTypeId);
        var room = await _repository.FindActiveAsync(id, tracking: true)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");
        EnsureVersion(room, version);

        if (await _repository.CodeExistsAsync(code, excludingId: id))
            throw new RoomException(RoomErrorKind.Conflict, "Mã phòng đã tồn tại.");

        // Phòng có lịch sử Reservation/Stay không được đổi mã hay loại phòng.
        if ((room.RoomCode != code || room.RoomTypeId != request.RoomTypeId) &&
            await _repository.HasReservationOrStayAsync(id))
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng có lịch sử đặt/lưu trú: không được đổi mã hoặc loại phòng.");

        var oldValues = RoomMapper.ToAuditSnapshot(room);
        room.RoomCode = code;
        room.RoomTypeId = request.RoomTypeId;
        room.FloorNumber = request.FloorNumber;
        room.Description = NormalizeDescription(request.Description);
        var audit = BuildAudit("Update", room, actorId, actorName, correlationId,
            oldValues, RoomMapper.ToAuditSnapshot(room));
        await SaveForMutationAsync(room, version, audit);
        return await GetByIdAsync(room.Id);
    }

    /// <summary>UC-F03-05 - Soft Delete; giữ lịch sử dữ liệu liên quan.</summary>
    public async Task DeleteAsync(int id, int? actorId, string? actorName, string? correlationId)
    {
        var room = await _repository.FindActiveAsync(id, tracking: true)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");
        if (await _repository.HasAnyRelatedRecordsAsync(id))
            throw new RoomException(RoomErrorKind.Conflict,
                "Không thể xóa phòng đã liên kết đặt phòng, lưu trú, công việc hoặc khóa phòng.");

        var oldVersion = (byte[])room.RowVersion.Clone();
        var oldValues = RoomMapper.ToAuditSnapshot(room);
        room.DeletedAt = DateTime.UtcNow;
        var audit = BuildAudit("SoftDelete", room, actorId, actorName, correlationId,
            oldValues, RoomMapper.ToAuditSnapshot(room));
        await SaveForMutationAsync(room, oldVersion, audit);
    }

    /// <summary>UC-F03-06 - Cập nhật trạng thái vận hành/vệ sinh theo business rules.</summary>
    public async Task<RoomDto> UpdateStatusAsync(int id, UpdateRoomStatusRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        if (!request.OperationalStatus.HasValue && !request.HousekeepingStatus.HasValue)
            throw new RoomException(RoomErrorKind.Invalid,
                "Phải cung cấp ít nhất một trạng thái cần cập nhật.");

        var version = ParseRowVersion(request.RowVersion);
        var room = await _repository.FindActiveAsync(id, tracking: true)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");
        EnsureVersion(room, version);

        var operational = request.OperationalStatus.HasValue
            ? (RoomOperationalStatus)request.OperationalStatus.Value : room.OperationalStatus;
        var housekeeping = request.HousekeepingStatus.HasValue
            ? (HousekeepingStatus)request.HousekeepingStatus.Value : room.HousekeepingStatus;
        ValidateStatuses((int)operational, (int)housekeeping);

        var deps = await _repository.GetStatusDependenciesAsync(id, DateTime.UtcNow);
        if (deps.IsOccupied && operational == RoomOperationalStatus.OutOfService)
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng đang có khách, không thể chuyển OutOfService.");
        if (request.OperationalStatus == (int)RoomOperationalStatus.Active &&
            (deps.IsBlocked || deps.MaintenanceInProgress))
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng đang bị khóa hoặc có công việc bảo trì chưa hoàn tất.");
        if (request.HousekeepingStatus == (int)HousekeepingStatus.Inspected &&
            (deps.IsOccupied || deps.IsBlocked || deps.HousekeepingInProgress || deps.MaintenanceInProgress))
            throw new RoomException(RoomErrorKind.Conflict,
                "Chưa thể xác nhận phòng Inspected khi có khách, đang khóa hoặc có task chưa hoàn tất.");

        var oldValues = RoomMapper.ToAuditSnapshot(room);
        room.OperationalStatus = operational;
        room.HousekeepingStatus = housekeeping;
        var audit = BuildAudit("UpdateStatus", room, actorId, actorName, correlationId,
            oldValues, RoomMapper.ToAuditSnapshot(room));
        await SaveForMutationAsync(room, version, audit);
        return await GetByIdAsync(room.Id);
    }

    private async Task EnsureRoomTypeExistsAsync(int id)
    {
        if (!await _repository.ActiveRoomTypeExistsAsync(id))
            throw new RoomException(RoomErrorKind.Invalid,
                "RoomTypeId không tồn tại hoặc loại phòng đã được lưu trữ.");
    }

    private static string NormalizeCode(string? value)
    {
        var code = (value ?? "").Trim().ToUpperInvariant();
        if (!Regex.IsMatch(code, "^[A-Z0-9_-]{1,20}$"))
            throw new RoomException(RoomErrorKind.Invalid,
                "RoomCode gồm 1-20 ký tự A-Z, 0-9, dấu gạch dưới hoặc gạch ngang.");
        return code;
    }

    private static string? NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateStatuses(int operational, int housekeeping)
    {
        if (operational is < 1 or > 2 || housekeeping is < 1 or > 4)
            throw new RoomException(RoomErrorKind.Invalid,
                "OperationalStatus phải 1-2 và HousekeepingStatus phải 1-4.");
    }

    private static byte[] ParseRowVersion(string? value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value ?? "");
            if (bytes.Length == 8) return bytes;
        }
        catch (FormatException) { }
        throw new RoomException(RoomErrorKind.Invalid,
            "RowVersion phải là chuỗi Base64 hợp lệ (8 bytes) lấy từ GET chi tiết.");
    }

    private static void EnsureVersion(Room room, byte[] version)
    {
        if (!room.RowVersion.SequenceEqual(version))
            throw new RoomException(RoomErrorKind.Conflict,
                "RowVersion đã cũ. Hãy GET lại phòng để lấy phiên bản mới nhất.");
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;

    private static bool IsForeignKeyError(DbUpdateException ex)
        => ex.InnerException is SqlException sql && sql.Number == 547;

    private async Task SaveForMutationAsync(Room room, byte[] originalVersion, AuditLog audit)
    {
        try
        {
            await _repository.SaveWithAuditAsync(room, originalVersion, audit);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng đã thay đổi đồng thời. Hãy GET lại RowVersion rồi thử lại.");
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new RoomException(RoomErrorKind.Conflict, "Mã phòng đã tồn tại.");
        }
        catch (DbUpdateException ex) when (IsForeignKeyError(ex))
        {
            throw new RoomException(RoomErrorKind.Conflict,
                "Dữ liệu tham chiếu đã thay đổi, không thể lưu phòng.");
        }
    }

    private static AuditLog BuildAudit(string action, Room room,
        int? actorId, string? actorName, string? correlationId,
        object? oldValues, object? newValues)
        => new()
        {
            ActorAccountId = actorId,
            ActorUsernameSnapshot = actorName?.Length > 50 ? actorName[..50] : actorName,
            Action = action,
            EntityType = "Room",
            EntityId = room.Id.ToString(),
            EntityCodeSnapshot = room.RoomCode,
            OccurredAt = DateTime.UtcNow,
            Summary = $"{action} room {room.RoomCode}",
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CorrelationId = correlationId
        };
}
