using HotelManagementSystem.Entities.Common;

namespace HotelManagementSystem.Entities;

public class HotelService : AuditableEntity
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Unit { get; set; } = default!;
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceUsage> ServiceUsages { get; set; } = new List<ServiceUsage>();
}

public class ServiceUsage : AuditableEntity
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public int HotelServiceId { get; set; }
    public string ServiceNameSnapshot { get; set; } = default!;
    public string UnitSnapshot { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime UsedAt { get; set; }
    public int RecordedByAccountId { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Stay Stay { get; set; } = default!;
    public HotelService HotelService { get; set; } = default!;
    public InvoiceLine? InvoiceLine { get; set; }
}