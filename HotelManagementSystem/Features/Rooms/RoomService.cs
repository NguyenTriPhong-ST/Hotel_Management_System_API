using System.Text.Json;
using System.Text.RegularExpressions;
using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Features.Rooms;

public enum RoomErrorKind { Invalid, NotFound, Conflict }

public sealed class RoomException : Exception
{
    public RoomErrorKind Kind { get; }

    public RoomException(RoomErrorKind kind, string message) : base(message)
        => Kind = kind;
}

public sealed class RoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db) => _db = db;

    /// <summary>
    /// UC-F03-01 - Thêm phòng; kiểm tra mã duy nhất, loại phòng và trạng thái hợp lệ.
    /// </summary>
    public async Task<RoomDto> CreateAsync(CreateRoomRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.RoomCode);
        ValidateStatuses(request.OperationalStatus, request.HousekeepingStatus);
        await EnsureRoomTypeExistsAsync(request.RoomTypeId);

        // Mã phòng là unique index toàn bảng (kể cả bản ghi đã xóa mềm).
        if (await _db.Rooms.AnyAsync(x => x.RoomCode == code))
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

        // Lưu room trước để lấy Id, sau đó ghi AuditLog trong cùng transaction.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Rooms.Add(room);
        try
        {
            await _db.SaveChangesAsync();
            WriteAudit("Create", room, actorId, actorName, correlationId,
                oldValues: null, newValues: Snapshot(room));
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
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

    /// <summary>
    /// UC-F03-02, UC-F03-07 - Xem, tìm kiếm, lọc, phân trang và sắp xếp danh sách phòng.
    /// Occupied tính từ Stays; Blocked tính từ RoomBlocks đang có hiệu lực.
    /// </summary>
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
            throw new RoomException(RoomErrorKind.Invalid, "Page hoặc PageSize không hợp lệ.");

        var now = DateTime.UtcNow;
        IQueryable<Room> query = _db.Rooms.AsNoTracking().Where(r => r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.RoomCode))
        {
            var keyword = request.RoomCode.Trim();
            query = query.Where(r => r.RoomCode.Contains(keyword));
        }
        if (request.RoomTypeId.HasValue)
            query = query.Where(r => r.RoomTypeId == request.RoomTypeId.Value);
        if (request.FloorNumber.HasValue)
            query = query.Where(r => r.FloorNumber == request.FloorNumber.Value);
        if (request.OperationalStatus.HasValue)
            query = query.Where(r => (int)r.OperationalStatus == request.OperationalStatus.Value);
        if (request.HousekeepingStatus.HasValue)
            query = query.Where(r => (int)r.HousekeepingStatus == request.HousekeepingStatus.Value);
        if (request.IsOccupied.HasValue)
            query = query.Where(r => _db.Stays.Any(s =>
                s.RoomId == r.Id && s.CheckedOutAt == null) == request.IsOccupied.Value);
        if (request.IsBlocked.HasValue)
            query = query.Where(r => _db.RoomBlocks.Any(b =>
                b.RoomId == r.Id && b.CancelledAt == null && b.ReleasedAt == null &&
                b.StartAt <= now && (b.EndAt == null || b.EndAt > now)) == request.IsBlocked.Value);

        var totalCount = await query.CountAsync();
        query = (sortBy, direction) switch
        {
            ("floor", "asc") => query.OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomCode),
            ("floor", "desc") => query.OrderByDescending(r => r.FloorNumber).ThenBy(r => r.RoomCode),
            ("code", "desc") => query.OrderByDescending(r => r.RoomCode).ThenBy(r => r.Id),
            _ => query.OrderBy(r => r.RoomCode).ThenBy(r => r.Id)
        };

        var rows = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                Room = r,
                TypeCode = r.RoomType.Code,
                TypeName = r.RoomType.Name,
                IsOccupied = _db.Stays.Any(s => s.RoomId == r.Id && s.CheckedOutAt == null),
                IsBlocked = _db.RoomBlocks.Any(b => b.RoomId == r.Id &&
                    b.CancelledAt == null && b.ReleasedAt == null &&
                    b.StartAt <= now && (b.EndAt == null || b.EndAt > now))
            }).ToListAsync();

        var items = rows.Select(x => Map(x.Room, x.TypeCode, x.TypeName,
            x.IsOccupied, x.IsBlocked)).ToList();
        return new RoomPagedDto(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// UC-F03-03 - Xem chi tiết phòng theo ID cùng loại phòng và trạng thái hiện tại.
    /// </summary>
    public async Task<RoomDto> GetByIdAsync(int id)
    {
        var now = DateTime.UtcNow;
        var row = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == id && r.DeletedAt == null)
            .Select(r => new
            {
                Room = r,
                TypeCode = r.RoomType.Code,
                TypeName = r.RoomType.Name,
                IsOccupied = _db.Stays.Any(s => s.RoomId == r.Id && s.CheckedOutAt == null),
                IsBlocked = _db.RoomBlocks.Any(b => b.RoomId == r.Id &&
                    b.CancelledAt == null && b.ReleasedAt == null &&
                    b.StartAt <= now && (b.EndAt == null || b.EndAt > now))
            }).FirstOrDefaultAsync();

        return row is null
            ? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.")
            : Map(row.Room, row.TypeCode, row.TypeName, row.IsOccupied, row.IsBlocked);
    }

    /// <summary>
    /// UC-F03-04 - Cập nhật thông tin phòng, không đổi trạng thái tại endpoint này.
    /// </summary>
    public async Task<RoomDto> UpdateAsync(int id, UpdateRoomRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        var code = NormalizeCode(request.RoomCode);
        var version = ParseRowVersion(request.RowVersion);
        await EnsureRoomTypeExistsAsync(request.RoomTypeId);

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");
        EnsureVersion(room, version);

        if (await _db.Rooms.AnyAsync(r => r.Id != id && r.RoomCode == code))
            throw new RoomException(RoomErrorKind.Conflict, "Mã phòng đã tồn tại.");

        // Bảo toàn lịch sử: không đổi mã/loại phòng nếu có reservation hoặc stay tham chiếu.
        if (room.RoomCode != code || room.RoomTypeId != request.RoomTypeId)
        {
            if (await _db.Reservations.AnyAsync(r => r.RoomId == id) ||
                await _db.Stays.AnyAsync(s => s.RoomId == id))
                throw new RoomException(RoomErrorKind.Conflict,
                    "Phòng có lịch sử đặt/lưu trú: không được đổi mã hoặc loại phòng.");
        }

        var oldValues = Snapshot(room);
        _db.Entry(room).Property(r => r.RowVersion).OriginalValue = version;
        room.RoomCode = code;
        room.RoomTypeId = request.RoomTypeId;
        room.FloorNumber = request.FloorNumber;
        room.Description = NormalizeDescription(request.Description);
        WriteAudit("Update", room, actorId, actorName, correlationId,
            oldValues, Snapshot(room));
        await SaveChangesForMutationAsync();
        return await GetByIdAsync(room.Id);
    }

    /// <summary>
    /// UC-F03-05 - Lưu trữ mềm phòng; từ chối nếu đã có dữ liệu nghiệp vụ liên quan.
    /// </summary>
    public async Task DeleteAsync(int id, int? actorId, string? actorName, string? correlationId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");

        if (await _db.Reservations.AnyAsync(r => r.RoomId == id) ||
            await _db.Stays.AnyAsync(s => s.RoomId == id) ||
            await _db.HotelTasks.AnyAsync(t => t.RoomId == id) ||
            await _db.RoomBlocks.AnyAsync(b => b.RoomId == id))
            throw new RoomException(RoomErrorKind.Conflict,
                "Không thể xóa phòng đã liên kết đặt phòng, lưu trú, công việc hoặc khóa phòng.");

        var oldValues = Snapshot(room);
        room.DeletedAt = DateTime.UtcNow;
        WriteAudit("SoftDelete", room, actorId, actorName, correlationId,
            oldValues, Snapshot(room));
        await SaveChangesForMutationAsync();
    }

    /// <summary>
    /// UC-F03-06 - Cập nhật trạng thái vận hành và/hoặc vệ sinh.
    /// Kiểm tra không mâu thuẫn với stay, maintenance block và công việc đang mở.
    /// </summary>
    public async Task<RoomDto> UpdateStatusAsync(int id, UpdateRoomStatusRequest request,
        int? actorId, string? actorName, string? correlationId)
    {
        if (!request.OperationalStatus.HasValue && !request.HousekeepingStatus.HasValue)
            throw new RoomException(RoomErrorKind.Invalid,
                "Phải cung cấp ít nhất một trạng thái cần cập nhật.");

        var version = ParseRowVersion(request.RowVersion);
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new RoomException(RoomErrorKind.NotFound, "Không tìm thấy phòng.");
        EnsureVersion(room, version);

        var operational = request.OperationalStatus.HasValue
            ? (RoomOperationalStatus)request.OperationalStatus.Value : room.OperationalStatus;
        var housekeeping = request.HousekeepingStatus.HasValue
            ? (HousekeepingStatus)request.HousekeepingStatus.Value : room.HousekeepingStatus;
        ValidateStatuses((int)operational, (int)housekeeping);

        var occupied = await _db.Stays.AnyAsync(s => s.RoomId == id && s.CheckedOutAt == null);
        var now = DateTime.UtcNow;
        var blocked = await _db.RoomBlocks.AnyAsync(b => b.RoomId == id &&
            b.CancelledAt == null && b.ReleasedAt == null &&
            b.StartAt <= now && (b.EndAt == null || b.EndAt > now));
        var maintenanceInProgress = await _db.HotelTasks.AnyAsync(t => t.RoomId == id &&
            t.TaskType == HotelTaskType.Maintenance &&
            t.Status != HotelTaskStatus.Completed && t.Status != HotelTaskStatus.Cancelled);
        var housekeepingInProgress = await _db.HotelTasks.AnyAsync(t => t.RoomId == id &&
            t.TaskType == HotelTaskType.Housekeeping &&
            t.Status != HotelTaskStatus.Completed && t.Status != HotelTaskStatus.Cancelled);

        if (occupied && operational == RoomOperationalStatus.OutOfService)
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng đang có khách, không thể chuyển OutOfService.");
        if (request.OperationalStatus == (int)RoomOperationalStatus.Active &&
            (blocked || maintenanceInProgress))
            throw new RoomException(RoomErrorKind.Conflict,
                "Phòng đang bị khóa hoặc có công việc bảo trì chưa hoàn tất.");
        if (request.HousekeepingStatus == (int)HousekeepingStatus.Inspected &&
            (occupied || blocked || housekeepingInProgress || maintenanceInProgress))
            throw new RoomException(RoomErrorKind.Conflict,
                "Chưa thể xác nhận phòng Inspected khi có khách, đang khóa hoặc có task chưa hoàn tất.");

        var oldValues = Snapshot(room);
        _db.Entry(room).Property(r => r.RowVersion).OriginalValue = version;
        room.OperationalStatus = operational;
        room.HousekeepingStatus = housekeeping;
        WriteAudit("UpdateStatus", room, actorId, actorName, correlationId,
            oldValues, Snapshot(room));
        await SaveChangesForMutationAsync();
        return await GetByIdAsync(room.Id);
    }

    private async Task EnsureRoomTypeExistsAsync(int roomTypeId)
    {
        if (!await _db.RoomTypes.AnyAsync(t => t.Id == roomTypeId && t.DeletedAt == null))
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

    private async Task SaveChangesForMutationAsync()
    {
        try { await _db.SaveChangesAsync(); }
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

    private static RoomDto Map(Room room, string typeCode, string typeName,
        bool isOccupied, bool isBlocked) => new(
        room.Id,
        room.RoomCode,
        room.RoomTypeId,
        typeCode,
        typeName,
        room.FloorNumber,
        room.Description,
        (int)room.OperationalStatus,
        (int)room.HousekeepingStatus,
        isOccupied,
        isBlocked,
        room.CreatedAt,
        room.UpdatedAt,
        Convert.ToBase64String(room.RowVersion));

    private static object Snapshot(Room room) => new
    {
        room.Id,
        room.RoomCode,
        room.RoomTypeId,
        room.FloorNumber,
        room.Description,
        room.OperationalStatus,
        room.HousekeepingStatus,
        room.DeletedAt
    };

    private void WriteAudit(string action, Room room, int? actorId, string? actorName,
        string? correlationId, object? oldValues, object? newValues)
    {
        _db.AuditLogs.Add(new AuditLog
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
        });
    }
}
