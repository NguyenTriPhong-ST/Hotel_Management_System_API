using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

/// <summary>
/// Interface định nghĩa các thao tác dữ liệu cho Entity AuthSession (quản lý phiên đăng nhập).
/// </summary>
public interface IAuthSessionRepository
{
    /// <summary>
    /// Lấy thông tin phiên đăng nhập còn hiệu lực theo SessionId (kèm theo thông tin tài khoản).
    /// </summary>
    Task<AuthSession?> GetActiveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm phiên đăng nhập mới.
    /// </summary>
    Task AddAsync(AuthSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi một phiên đăng nhập cụ thể (RevokedAt = UTC now).
    /// </summary>
    Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi tất cả các phiên đăng nhập đang hoạt động của một tài khoản (khi đổi role hoặc khóa tài khoản).
    /// </summary>
    Task RevokeAllSessionsByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu các thay đổi xuống CSDL.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
