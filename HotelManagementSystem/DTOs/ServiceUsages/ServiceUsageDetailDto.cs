
namespace HotelManagementSystem.DTOs.ServiceUsages;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-03 - View Service Usage Details
// Mô tả: DTO trả thông tin chi tiết lượt sử dụng.
// OData: Không sử dụng vì truy vấn theo ID.
// =====================================================

public class ServiceUsageDetailDto
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
    public string RecordedByName { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
