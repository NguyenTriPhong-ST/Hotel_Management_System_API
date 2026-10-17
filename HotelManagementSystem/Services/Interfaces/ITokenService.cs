using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface định nghĩa dịch vụ tạo và xác thực token JWT.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Phát hành token JWT mới chứa thông tin tài khoản, vai trò và SessionId.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateToken(Account account, Guid sessionId);
}
