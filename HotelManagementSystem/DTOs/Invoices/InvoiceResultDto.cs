
namespace HotelManagementSystem.DTOs.Invoices;

// Developer: NhaNTCE181298
// UC-F07-04: Kết quả tính phí và chốt hóa đơn.

public class InvoiceLineResultDto
{
    public int Id { get; set; }
    public string LineType { get; set; } = "";
    public int? ServiceUsageId { get; set; }
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceResultDto
{
    public int Id { get; set; }
    public string InvoiceCode { get; set; } = "";
    public int StayId { get; set; }
    public string StayCode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly BillingFromDate { get; set; }
    public DateOnly BillingToDate { get; set; }
    public string GuestName { get; set; } = "";
    public string RoomCode { get; set; } = "";
    public decimal RoomAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public List<InvoiceLineResultDto> Lines { get; set; } = new();
}
