using HotelManagementSystem.DTOs.Accounts;

namespace HotelManagementSystem.DTOs.Auth;

/// <summary>
/// DTO phản hồi sau khi đăng nhập thành công.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Chuỗi JWT token dùng để xác thực các request kế tiếp.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm hết hạn của token (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Mã phiên đăng nhập được cấp.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Thông tin tài khoản đăng nhập.
    /// </summary>
    public AccountDto Account { get; set; } = default!;
}
