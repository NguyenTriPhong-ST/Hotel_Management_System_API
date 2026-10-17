using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.HotelServices;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

/// <summary>
/// Triển khai truy vấn dữ liệu cho HotelService qua EF Core AppDbContext.
/// </summary>
public class HotelServiceRepository : IHotelServiceRepository
{
    private readonly AppDbContext _context;

    public HotelServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HotelService?> GetByIdAsync(int id, bool asNoTracking = false, bool includeUsages = false)
    {
        IQueryable<HotelService> query = _context.HotelServices;
        if (asNoTracking) query = query.AsNoTracking();

        if (includeUsages)
        {
            query = query.Include(s => s.ServiceUsages);
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<(List<HotelService> Items, int TotalCount)> GetPagedAsync(HotelServiceQueryParameters parameters)
    {
        var query = _context.HotelServices.AsNoTracking();

        // 1. Tìm kiếm theo từ khóa
        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            var keyword = parameters.Keyword.Trim();
            query = query.Where(s => s.Name.Contains(keyword) || s.ServiceCode.Contains(keyword));
        }

        // 2. Lọc theo trạng thái
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == parameters.IsActive.Value);
        }

        // 3. Lọc theo khoảng giá
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(s => s.CurrentPrice >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(s => s.CurrentPrice <= parameters.MaxPrice.Value);
        }

        var totalCount = await query.CountAsync();

        // 4. Sắp xếp
        var isAsc = string.Equals(parameters.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        query = (parameters.SortBy?.ToLower()) switch
        {
            "currentprice" or "price" => isAsc ? query.OrderBy(s => s.CurrentPrice) : query.OrderByDescending(s => s.CurrentPrice),
            "servicecode" => isAsc ? query.OrderBy(s => s.ServiceCode) : query.OrderByDescending(s => s.ServiceCode),
            "createdat" => isAsc ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt),
            _ => isAsc ? query.OrderBy(s => s.Name) : query.OrderByDescending(s => s.Name)
        };

        // 5. Phân trang
        var pageIndex = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.ServiceUsages)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsServiceCodeExistsAsync(string serviceCode, int? excludeId = null)
    {
        var code = serviceCode.Trim().ToUpper();
        var query = _context.HotelServices.Where(s => s.ServiceCode == code);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasUsagesAsync(int id)
    {
        return await _context.ServiceUsages.AnyAsync(su => su.HotelServiceId == id);
    }

    public async Task AddAsync(HotelService service)
    {
        await _context.HotelServices.AddAsync(service);
    }

    public Task UpdateAsync(HotelService service)
    {
        _context.HotelServices.Update(service);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(HotelService service)
    {
        _context.HotelServices.Remove(service);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
