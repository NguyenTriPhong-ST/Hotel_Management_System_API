
using System.ComponentModel.DataAnnotations;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Payments;

// Developer: NhaNTCE181298
// UC-F07-05: Record Internal Payment

public class RecordPaymentRequest
{
    [Required]
    public Guid? IdempotencyKey { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Amount phải lớn hơn 0."
    )]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod? Method { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class PaymentResultDto
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = "";
    public int InvoiceId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public DateTime PaidAt { get; set; }
    public int RecordedByAccountId { get; set; }

    public decimal InvoiceTotal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Message { get; set; } = "";
}
