using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

/// <summary>
/// Triển khai repository truy vấn và thao tác trên bảng Accounts.
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy tài khoản theo ID.
    /// </summary>
    public async Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>
    /// Lấy tài khoản theo username đã chuẩn hóa.
    /// </summary>
    public async Task<Account?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.NormalizedUsername == normalizedUsername, cancellationToken);
    }

    /// <summary>
    /// Kiểm tra trùng username đã chuẩn hóa.
    /// </summary>
    public async Task<bool> ExistsByNormalizedUsernameAsync(string normalizedUsername, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Accounts.AsNoTracking().Where(a => a.NormalizedUsername == normalizedUsername);
        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy danh sách tài khoản có phân trang, lọc và sắp xếp.
    /// </summary>
    public async Task<(IReadOnlyList<Account> Items, int TotalCount)> GetPagedListAsync(AccountFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Accounts.AsNoTracking().AsQueryable();

        // Lọc theo từ khóa tìm kiếm (username hoặc tên nhân viên)
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(a => a.Username.Contains(keyword) || a.FullName.Contains(keyword));
        }

        // Lọc theo vai trò
        if (filter.Role.HasValue)
        {
            query = query.Where(a => a.Role == filter.Role.Value);
        }

        // Lọc theo trạng thái hoạt động
        if (filter.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sắp xếp
        var sortBy = (filter.SortBy ?? "username").ToLowerInvariant();
        query = (sortBy, filter.SortDescending) switch
        {
            ("createdat", false) => query.OrderBy(a => a.CreatedAt),
            ("createdat", true) => query.OrderByDescending(a => a.CreatedAt),
            ("username", true) => query.OrderByDescending(a => a.Username),
            _ => query.OrderBy(a => a.Username)
        };

        // Phân trang
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Thêm tài khoản mới.
    /// </summary>
    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
    }

    /// <summary>
    /// Cập nhật tài khoản.
    /// </summary>
    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lưu thay đổi vào CSDL.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
