using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// Yêu cầu cập nhật thông tin tài khoản nhân viên (Admin).
/// </summary>
public class UpdateAccountRequestDto
{
    /// <summary>
    /// Tên đăng nhập mới (nếu muốn thay đổi).
    /// </summary>
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số và ký tự . _ -")]
    public string? Username { get; set; }

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
}
