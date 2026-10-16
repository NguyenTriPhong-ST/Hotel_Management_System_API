
namespace HotelManagementSystem.DTOs.Stays;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-01 - Check-in
// Mô tả: Trả thông tin Stay sau khi nhận phòng.
// =====================================================

public class CheckInResponse
{
    public int StayId { get; set; }
    public string StayCode { get; set; } = string.Empty;

    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;

    public int MainGuestId { get; set; }
    public List<int> GuestIds { get; set; } = new();

    public DateTime CheckedInAt { get; set; }
    public int CheckedInByAccountId { get; set; }

    public string ReservationStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
