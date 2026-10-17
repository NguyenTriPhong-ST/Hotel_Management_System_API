using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class Reservation : AuditableEntity
{
    public int Id { get; set; }
    public string ReservationCode { get; set; } = default!;
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public int CapacitySnapshot { get; set; }
    public decimal NightlyRate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public string? Notes { get; set; }
    public int CreatedByAccountId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledByAccountId { get; set; }
    public string? CancellationReason { get; set; }

    public Guest Guest { get; set; } = default!;
    public Room Room { get; set; } = default!;
    public RoomType RoomType { get; set; } = default!;
    public Stay? Stay { get; set; }
}