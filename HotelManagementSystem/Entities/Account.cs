using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class Account : AuditableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;
    public string NormalizedUsername { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public AccountRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}