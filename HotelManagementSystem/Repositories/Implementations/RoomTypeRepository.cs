using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

public sealed class RoomTypeRepository : IRoomTypeRepository
{
    private readonly AppDbContext _db;

    public RoomTypeRepository(AppDbContext db) => _db = db;

    public Task<bool> CodeExistsAsync(string code, int? excludingId = null)
        => _db.RoomTypes.AnyAsync(x => x.Code == code &&
            (!excludingId.HasValue || x.Id != excludingId.Value));

    public Task<RoomType?> FindActiveAsync(int id, bool tracking = false)
    {
        IQueryable<RoomType> query = _db.RoomTypes.Where(x => x.Id == id && x.DeletedAt == null);
        return (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync();
    }

    public Task<bool> HasActiveRoomsAsync(int roomTypeId)
        => _db.Rooms.AnyAsync(x => x.RoomTypeId == roomTypeId && x.DeletedAt == null);

    public async Task<(IReadOnlyList<RoomType> Items, int TotalCount)> GetPagedAsync(
        RoomTypeSearchOptions options)
    {
        IQueryable<RoomType> query = _db.RoomTypes.AsNoTracking()
            .Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(options.Name))
        {
            var keyword = options.Name.Trim();
            query = query.Where(x => x.Name.Contains(keyword));
        }
        if (options.MinCapacity.HasValue)
            query = query.Where(x => x.Capacity >= options.MinCapacity.Value);
        if (options.MinPrice.HasValue)
            query = query.Where(x => x.BasePricePerNight >= options.MinPrice.Value);
        if (options.MaxPrice.HasValue)
            query = query.Where(x => x.BasePricePerNight <= options.MaxPrice.Value);

        var count = await query.CountAsync();
        query = (options.SortBy, options.SortDirection) switch
        {
            ("price", "asc") => query.OrderBy(x => x.BasePricePerNight).ThenBy(x => x.Id),
            ("price", "desc") => query.OrderByDescending(x => x.BasePricePerNight).ThenBy(x => x.Id),
            ("name", "desc") => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };

        var rows = await query.Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize).ToListAsync();
        return (rows, count);
    }

    public async Task InsertWithAuditAsync(RoomType entity, Func<RoomType, AuditLog> buildAudit)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync(); // Lấy Id và RowVersion do SQL Server tạo.
        _db.AuditLogs.Add(buildAudit(entity));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task SaveWithAuditAsync(RoomType entity, byte[] originalRowVersion, AuditLog audit)
    {
        _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = originalRowVersion;
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();
    }
}
