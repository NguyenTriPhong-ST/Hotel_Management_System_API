namespace HotelManagementSystem.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? ActorAccountId { get; set; }
    public string? ActorUsernameSnapshot { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string? EntityCodeSnapshot { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Summary { get; set; } = default!;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? CorrelationId { get; set; }

    public Account? ActorAccount { get; set; }
}