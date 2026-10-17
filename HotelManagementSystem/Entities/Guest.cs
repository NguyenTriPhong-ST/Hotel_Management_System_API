using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class Guest : AuditableEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentCountryCode { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<StayGuest> StayGuests { get; set; } = new List<StayGuest>();
}