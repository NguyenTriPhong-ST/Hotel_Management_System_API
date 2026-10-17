using HotelManagementSystem.Entities.Common;

namespace HotelManagementSystem.Entities;

public class RoomType : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}