
namespace HotelManagementSystem.DTOs.Stays;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-02 - View Stays
// Mô tả: Trả thông tin Stay, phòng, khách đại diện
// và trạng thái lưu trú.
// =====================================================

public class StayListDto
{
    public int Id { get; set; }
    public string StayCode { get; set; } = string.Empty;

    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;

    public int MainGuestId { get; set; }
    public string MainGuestName { get; set; } = string.Empty;

    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }

    public string Status { get; set; } = string.Empty;
}
