using System.ComponentModel.DataAnnotations;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// Yêu cầu thay đổi vai trò tài khoản nhân viên (Admin).
/// </summary>
public class UpdateRoleRequestDto
{
    /// <summary>
    /// Vai trò mới: 1 = Admin, 2 = Receptionist, 3 = StaffRoom.
    /// </summary>
    [Required(ErrorMessage = "Vai trò không được để trống.")]
    public AccountRole Role { get; set; }
}
