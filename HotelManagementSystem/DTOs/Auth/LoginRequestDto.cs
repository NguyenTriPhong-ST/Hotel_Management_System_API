using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.Auth;

/// <summary>
/// Yêu cầu đăng nhập vào hệ thống.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Tên đăng nhập của tài khoản.
    /// </summary>
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đăng nhập.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;
}
