using System.ComponentModel.DataAnnotations;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// Yêu cầu tạo mới tài khoản nhân viên (Admin).
/// </summary>
public class CreateAccountRequestDto
{
    /// <summary>
    /// Tên đăng nhập duy nhất.
    /// </summary>
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số và ký tự . _ -")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu ban đầu.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có tối thiểu 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Họ và tên nhân viên.
    /// </summary>
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Số điện thoại liên hệ.
    /// </summary>
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email liên hệ.
    /// </summary>
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    /// <summary>
    /// Vai trò của nhân viên: 1 = Admin, 2 = Receptionist, 3 = StaffRoom.
    /// </summary>
    [Required(ErrorMessage = "Vai trò không được để trống.")]
    public AccountRole Role { get; set; }
}
