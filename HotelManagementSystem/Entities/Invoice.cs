using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class Invoice : AuditableEntity
{
    public int Id { get; set; }
    public string InvoiceCode { get; set; } = default!;
    public int StayId { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateOnly BillingFromDate { get; set; }
    public DateOnly BillingToDate { get; set; }
    public string GuestNameSnapshot { get; set; } = default!;
    public string? GuestDocumentSnapshot { get; set; }
    public string RoomCodeSnapshot { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public int CreatedByAccountId { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByAccountId { get; set; }
    public string? Notes { get; set; }

    public Stay Stay { get; set; } = default!;
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class InvoiceLine : AuditableEntity
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public InvoiceLineType LineType { get; set; }
    public int? ServiceUsageId { get; set; }
    public string Description { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; private set; }   // cột tính toán, cấu hình ở bước Fluent API

    public Invoice Invoice { get; set; } = default!;
    public ServiceUsage? ServiceUsage { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = default!;
    public int InvoiceId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public string? ReferenceNumber { get; set; }
    public int RecordedByAccountId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }

    public Invoice Invoice { get; set; } = default!;
}