using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

/// <summary>
/// Triển khai repository quản lý phiên đăng nhập trên bảng AuthSessions.
/// </summary>
public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AppDbContext _context;

    public AuthSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy phiên đăng nhập còn hiệu lực theo SessionId (RevokedAt == null và chưa hết hạn).
    /// Kèm theo thông tin tài khoản để kiểm tra trạng thái tài khoản.
    /// </summary>
    public async Task<AuthSession?> GetActiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.AuthSessions
            .Include(s => s.Account)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.RevokedAt == null && s.ExpiresAt > now, cancellationToken);
    }

    /// <summary>
    /// Thêm phiên đăng nhập mới.
    /// </summary>
    public async Task AddAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        await _context.AuthSessions.AddAsync(session, cancellationToken);
    }

    /// <summary>
    /// Thu hồi một phiên đăng nhập cụ thể (RevokedAt = DateTime.UtcNow).
    /// </summary>
    public async Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.AuthSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null)
            return false;

        session.RevokedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Thu hồi toàn bộ phiên đăng nhập đang hoạt động của một tài khoản.
    /// Áp dụng khi đổi vai trò (role) hoặc khóa tài khoản (IsActive = false).
    /// </summary>
    public async Task RevokeAllSessionsByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var activeSessions = await _context.AuthSessions
            .Where(s => s.AccountId == accountId && s.RevokedAt == null && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAt = now;
        }
    }

    /// <summary>
    /// Lưu thay đổi vào CSDL.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
