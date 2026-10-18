
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.Stays;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-01 - Check-in
// Mô tả: Nhận thông tin đặt phòng và danh sách
// khách thực tế đến nhận phòng.
// =====================================================

public class CheckInRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> GuestIds { get; set; } = new();

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
