using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

public sealed class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db) => _db = db;

    public Task<bool> CodeExistsAsync(string roomCode, int? excludingId = null)
        => _db.Rooms.AnyAsync(r => r.RoomCode == roomCode &&
            (!excludingId.HasValue || r.Id != excludingId.Value));

    public Task<bool> ActiveRoomTypeExistsAsync(int roomTypeId)
        => _db.RoomTypes.AnyAsync(t => t.Id == roomTypeId && t.DeletedAt == null);

    public Task<Room?> FindActiveAsync(int id, bool tracking = false)
    {
        IQueryable<Room> query = _db.Rooms.Where(r => r.Id == id && r.DeletedAt == null);
        return (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync();
    }

    /// <summary>UC-F03-02, UC-F03-07: Query, search, filter, pagination; tính Occupied/Blocked từ bảng nghiệp vụ.</summary>
    public async Task<(IReadOnlyList<RoomDataView> Items, int TotalCount)> GetPagedAsync(RoomSearchOptions o)
    {
        IQueryable<Room> query = _db.Rooms.AsNoTracking()
            .Where(r => r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(o.RoomCode))
        {
            var code = o.RoomCode.Trim();
            query = query.Where(r => r.RoomCode.Contains(code));
        }
        if (o.RoomTypeId.HasValue)
            query = query.Where(r => r.RoomTypeId == o.RoomTypeId.Value);
        if (o.FloorNumber.HasValue)
            query = query.Where(r => r.FloorNumber == o.FloorNumber.Value);
        if (o.OperationalStatus.HasValue)
            query = query.Where(r => (int)r.OperationalStatus == o.OperationalStatus.Value);
        if (o.HousekeepingStatus.HasValue)
            query = query.Where(r => (int)r.HousekeepingStatus == o.HousekeepingStatus.Value);
        if (o.IsOccupied.HasValue)
            query = query.Where(r => _db.Stays.Any(s =>
                s.RoomId == r.Id && s.CheckedOutAt == null) == o.IsOccupied.Value);
        if (o.IsBlocked.HasValue)
            query = query.Where(r => _db.RoomBlocks.Any(b =>
                b.RoomId == r.Id && b.CancelledAt == null && b.ReleasedAt == null &&
                b.StartAt <= o.NowUtc && (b.EndAt == null || b.EndAt > o.NowUtc)) == o.IsBlocked.Value);

        var count = await query.CountAsync();
        query = (o.SortBy, o.SortDirection) switch
        {
            ("floor", "asc") => query.OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomCode),
            ("floor", "desc") => query.OrderByDescending(r => r.FloorNumber).ThenBy(r => r.RoomCode),
            ("code", "desc") => query.OrderByDescending(r => r.RoomCode).ThenBy(r => r.Id),
            _ => query.OrderBy(r => r.RoomCode).ThenBy(r => r.Id)
        };

        var rows = await query.Skip((o.Page - 1) * o.PageSize)
            .Take(o.PageSize)
            .Select(r => new
            {
                Room = r,
                RoomTypeCode = r.RoomType.Code,
                RoomTypeName = r.RoomType.Name,
                IsOccupied = _db.Stays.Any(s => s.RoomId == r.Id && s.CheckedOutAt == null),
                IsBlocked = _db.RoomBlocks.Any(b => b.RoomId == r.Id &&
                    b.CancelledAt == null && b.ReleasedAt == null &&
                    b.StartAt <= o.NowUtc && (b.EndAt == null || b.EndAt > o.NowUtc))
            }).ToListAsync();

        return (rows.Select(x => new RoomDataView(x.Room, x.RoomTypeCode, x.RoomTypeName,
            x.IsOccupied, x.IsBlocked)).ToList(), count);
    }

    /// <summary>UC-F03-03: chi tiết phòng cùng RoomType, occupancy/block trạng thái hiện tại.</summary>
    public async Task<RoomDataView?> GetDetailAsync(int id, DateTime nowUtc)
    {
        var row = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == id && r.DeletedAt == null)
            .Select(r => new
            {
                Room = r,
                RoomTypeCode = r.RoomType.Code,
                RoomTypeName = r.RoomType.Name,
                IsOccupied = _db.Stays.Any(s => s.RoomId == r.Id && s.CheckedOutAt == null),
                IsBlocked = _db.RoomBlocks.Any(b => b.RoomId == r.Id &&
                    b.CancelledAt == null && b.ReleasedAt == null &&
                    b.StartAt <= nowUtc && (b.EndAt == null || b.EndAt > nowUtc))
            }).FirstOrDefaultAsync();

        return row is null ? null : new RoomDataView(row.Room, row.RoomTypeCode,
            row.RoomTypeName, row.IsOccupied, row.IsBlocked);
    }

    // Bảo toàn lịch sử, kể cả bản ghi đặt phòng/stay trước đó.
    public async Task<bool> HasReservationOrStayAsync(int roomId)
        => await _db.Reservations.AnyAsync(r => r.RoomId == roomId) ||
           await _db.Stays.AnyAsync(s => s.RoomId == roomId);

    public async Task<bool> HasAnyRelatedRecordsAsync(int roomId)
        => await HasReservationOrStayAsync(roomId) ||
           await _db.HotelTasks.AnyAsync(t => t.RoomId == roomId) ||
           await _db.RoomBlocks.AnyAsync(b => b.RoomId == roomId);

    public async Task<RoomStatusDependencies> GetStatusDependenciesAsync(int roomId, DateTime nowUtc)
    {
        var occupied = await _db.Stays.AnyAsync(s => s.RoomId == roomId && s.CheckedOutAt == null);
        var blocked = await _db.RoomBlocks.AnyAsync(b => b.RoomId == roomId &&
            b.CancelledAt == null && b.ReleasedAt == null &&
            b.StartAt <= nowUtc && (b.EndAt == null || b.EndAt > nowUtc));
        var maintenance = await _db.HotelTasks.AnyAsync(t => t.RoomId == roomId &&
            t.TaskType == HotelTaskType.Maintenance &&
            t.Status != HotelTaskStatus.Completed && t.Status != HotelTaskStatus.Cancelled);
        var housekeeping = await _db.HotelTasks.AnyAsync(t => t.RoomId == roomId &&
            t.TaskType == HotelTaskType.Housekeeping &&
            t.Status != HotelTaskStatus.Completed && t.Status != HotelTaskStatus.Cancelled);
        return new RoomStatusDependencies(occupied, blocked, maintenance, housekeeping);
    }

    public async Task InsertWithAuditAsync(Room entity, Func<Room, AuditLog> buildAudit)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync(); // Lấy Id/RowVersion do SQL Server tạo.
        _db.AuditLogs.Add(buildAudit(entity));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task SaveWithAuditAsync(Room entity, byte[] originalRowVersion, AuditLog audit)
    {
        _db.Entry(entity).Property(r => r.RowVersion).OriginalValue = originalRowVersion;
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();
    }
}
