using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Guests;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

/// <summary>
/// Triển khai truy vấn dữ liệu cho Guest qua EF Core AppDbContext.
/// </summary>
public class GuestRepository : IGuestRepository
{
    private readonly AppDbContext _context;

    public GuestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guest?> GetByIdAsync(int id, bool asNoTracking = false, bool includeHistory = false)
    {
        IQueryable<Guest> query = _context.Guests;
        if (asNoTracking) query = query.AsNoTracking();

        if (includeHistory)
        {
            query = query
                .Include(g => g.Reservations)
                    .ThenInclude(r => r.Room)
                .Include(g => g.Reservations)
                    .ThenInclude(r => r.RoomType)
                .Include(g => g.StayGuests)
                    .ThenInclude(sg => sg.Stay)
                        .ThenInclude(s => s.Room);
        }

        return await query.FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);
    }

    public async Task<(List<Guest> Items, int TotalCount)> GetPagedAsync(GuestQueryParameters parameters)
    {
        var query = _context.Guests
            .AsNoTracking()
            .Where(g => g.DeletedAt == null);

        // 1. Tìm kiếm theo từ khóa
        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            var keyword = parameters.Keyword.Trim();
            query = query.Where(g =>
                g.FullName.Contains(keyword) ||
                (g.PhoneNumber != null && g.PhoneNumber.Contains(keyword)) ||
                (g.Email != null && g.Email.Contains(keyword)) ||
                (g.DocumentNumber != null && g.DocumentNumber.Contains(keyword)));
        }

        // 2. Lọc theo họ tên
        if (!string.IsNullOrWhiteSpace(parameters.FullName))
        {
            var name = parameters.FullName.Trim();
            query = query.Where(g => g.FullName.Contains(name));
        }

        // 3. Lọc theo số điện thoại
        if (!string.IsNullOrWhiteSpace(parameters.PhoneNumber))
        {
            var phone = parameters.PhoneNumber.Trim();
            query = query.Where(g => g.PhoneNumber != null && g.PhoneNumber.Contains(phone));
        }

        // 4. Lọc theo số giấy tờ
        if (!string.IsNullOrWhiteSpace(parameters.DocumentNumber))
        {
            var docNum = parameters.DocumentNumber.Trim();
            query = query.Where(g => g.DocumentNumber != null && g.DocumentNumber.Contains(docNum));
        }

        // 5. Lọc theo lịch sử lưu trú
        if (parameters.HasStayHistory.HasValue)
        {
            if (parameters.HasStayHistory.Value)
                query = query.Where(g => g.StayGuests.Any());
            else
                query = query.Where(g => !g.StayGuests.Any());
        }

        var totalCount = await query.CountAsync();

        // 6. Sắp xếp
        var isAsc = string.Equals(parameters.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        query = (parameters.SortBy?.ToLower()) switch
        {
            "fullname" => isAsc ? query.OrderBy(g => g.FullName) : query.OrderByDescending(g => g.FullName),
            "dateofbirth" => isAsc ? query.OrderBy(g => g.DateOfBirth) : query.OrderByDescending(g => g.DateOfBirth),
            _ => isAsc ? query.OrderBy(g => g.CreatedAt) : query.OrderByDescending(g => g.CreatedAt)
        };

        // 7. Phân trang
        var pageIndex = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Include(g => g.Reservations)
            .Include(g => g.StayGuests)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsDocumentExistsAsync(DocumentType? type, string? countryCode, string? number, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(number) || !type.HasValue) return false;

        var country = countryCode?.Trim().ToUpper();
        var docNum = number.Trim();

        var query = _context.Guests.Where(g =>
            g.DeletedAt == null &&
            g.DocumentType == type &&
            g.DocumentCountryCode == country &&
            g.DocumentNumber == docNum);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasActiveReservationsAsync(int guestId)
    {
        return await _context.Reservations.AnyAsync(r =>
            r.GuestId == guestId &&
            (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn));
    }

    public async Task<bool> IsCurrentlyStayingAsync(int guestId)
    {
        return await _context.StayGuests.AnyAsync(sg =>
            sg.GuestId == guestId &&
            sg.Stay.CheckedOutAt == null);
    }

    public async Task AddAsync(Guest guest)
    {
        await _context.Guests.AddAsync(guest);
    }

    public Task UpdateAsync(Guest guest)
    {
        _context.Guests.Update(guest);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
