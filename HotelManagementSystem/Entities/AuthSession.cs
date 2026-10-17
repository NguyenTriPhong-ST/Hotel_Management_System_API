namespace HotelManagementSystem.Entities;

public class AuthSession
{
    public Guid Id { get; set; }
    public int AccountId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Account Account { get; set; } = default!;
}