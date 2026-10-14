using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class Room : AuditableEntity
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = default!;
    public int RoomTypeId { get; set; }
    public short FloorNumber { get; set; }
    public string? Description { get; set; }
    public RoomOperationalStatus OperationalStatus { get; set; } = RoomOperationalStatus.Active;
    public HousekeepingStatus HousekeepingStatus { get; set; } = HousekeepingStatus.Dirty;
    public DateTime? DeletedAt { get; set; }

    public RoomType RoomType { get; set; } = default!;
}