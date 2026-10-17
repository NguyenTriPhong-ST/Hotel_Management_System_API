using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// Yêu cầu kích hoạt hoặc vô hiệu hóa tài khoản (Admin).
/// </summary>
public class UpdateStatusRequestDto
{
    /// <summary>
    /// Trạng thái hoạt động: true = Hoạt động, false = Khóa/Vô hiệu hóa.
    /// </summary>
    [Required(ErrorMessage = "Trạng thái không được để trống.")]
    public bool IsActive { get; set; }
}
