
namespace HotelManagementSystem.DTOs.ServiceUsages;

// Developer: NhaNTCE181298
// UC-F09-01: Kết quả ghi nhận sử dụng dịch vụ.

public class CreateServiceUsageResponse
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public int HotelServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public DateTime UsedAt { get; set; }
    public int RecordedByAccountId { get; set; }
    public string? Notes { get; set; }

    public string Message { get; set; } = string.Empty;
}
