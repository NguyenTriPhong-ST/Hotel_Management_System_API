using HotelManagementSystem.Entities.Common;

namespace HotelManagementSystem.Entities;

public class Stay : AuditableEntity
{
    public int Id { get; set; }
    public string StayCode { get; set; } = default!;
    public int ReservationId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int CheckedInByAccountId { get; set; }
    public int? CheckedOutByAccountId { get; set; }
    public string? Notes { get; set; }

    public Reservation Reservation { get; set; } = default!;
    public Room Room { get; set; } = default!;
    public ICollection<StayGuest> StayGuests { get; set; } = new List<StayGuest>();
    public ICollection<ServiceUsage> ServiceUsages { get; set; } = new List<ServiceUsage>();
    public Invoice? Invoice { get; set; }
}

public class StayGuest
{
    public int StayId { get; set; }
    public int GuestId { get; set; }
    public bool IsMainGuest { get; set; }
    public DateTime AddedAt { get; set; }

    public Stay Stay { get; set; } = default!;
    public Guest Guest { get; set; } = default!;
}