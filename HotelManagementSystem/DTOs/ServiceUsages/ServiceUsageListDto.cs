
namespace HotelManagementSystem.DTOs.ServiceUsages;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-02 - View Service Usages
// OData: DTO dùng để lọc, sắp xếp và phân trang.
// =====================================================

public class ServiceUsageListDto
{
    public int Id { get; set; }

    public int StayId { get; set; }
    public string StayCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;

    public int HotelServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public DateTime UsedAt { get; set; }
    public int RecordedByAccountId { get; set; }

    public string? Notes { get; set; }
}
