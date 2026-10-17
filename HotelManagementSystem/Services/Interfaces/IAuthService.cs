using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.DTOs.Auth;

namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface định nghĩa nghiệp vụ xác thực người dùng (F01).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// UC-F01-01 - Đăng nhập.
    /// Kiểm tra username, password và trạng thái tài khoản; tạo phiên và cấp JWT.
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F01-02 - Đăng xuất.
    /// Thu hồi phiên làm việc hiện tại, làm token mất hiệu lực.
    /// </summary>
    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại từ ID.
    /// </summary>
    Task<AccountDto> GetCurrentAccountAsync(int accountId, CancellationToken cancellationToken = default);
}
