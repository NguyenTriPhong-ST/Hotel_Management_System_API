
namespace HotelManagementSystem.DTOs.Stays;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-03 - View Stay Details
// Mô tả: DTO trả thông tin chi tiết một Stay.
// OData: Không sử dụng vì truy vấn theo ID.
// =====================================================

public class StayDetailDto
{
    public int Id { get; set; }
    public string StayCode { get; set; } = string.Empty;

    // Reservation
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal NightlyRate { get; set; }

    // Room
    public int RoomId { get; set; }
    // Developer: NhaNTCE181298
    // UC-F07-03: Khởi tạo RoomCode để tránh CS8618.
    public string RoomCode { get; set; } = string.Empty;

    // Stay
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int CheckedInByAccountId { get; set; }
    public int? CheckedOutByAccountId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;

    // Related data
    public List<StayGuestDetailDto> Guests { get; set; } = new();
    public List<StayServiceUsageDetailDto> ServiceUsages { get; set; } = new();
}

public class StayGuestDetailDto
{
    public int GuestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsMainGuest { get; set; }
}

public class StayServiceUsageDetailDto
{
    public int Id { get; set; }
    public int HotelServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public DateTime UsedAt { get; set; }
}
