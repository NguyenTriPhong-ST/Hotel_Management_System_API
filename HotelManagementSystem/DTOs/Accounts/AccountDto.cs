using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// DTO thông tin tài khoản nhân viên (không chứa PasswordHash).
/// </summary>
public class AccountDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public AccountRole Role { get; set; }
    public string RoleName => Role.ToString();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
