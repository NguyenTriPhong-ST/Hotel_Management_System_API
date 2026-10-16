
namespace HotelManagementSystem.DTOs.Stays;

// Developer: NhaNTCE181298
// UC-F07-06: Check-out

public class CheckOutResponse
{
    public int StayId { get; set; }
    public string StayCode { get; set; } = "";
    public int ReservationId { get; set; }
    public int RoomId { get; set; }
    public string RoomCode { get; set; } = "";
    public DateTime CheckedOutAt { get; set; }
    public int CheckedOutByAccountId { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal TotalPaid { get; set; }
    public string ReservationStatus { get; set; } = "";
    public string HousekeepingStatus { get; set; } = "";
    public string Message { get; set; } = "";
}
