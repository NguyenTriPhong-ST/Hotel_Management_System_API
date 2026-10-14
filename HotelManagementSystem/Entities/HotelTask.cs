using HotelManagementSystem.Entities.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Entities;

public class HotelTask : AuditableEntity
{
    public int Id { get; set; }
    public string TaskCode { get; set; } = default!;
    public int RoomId { get; set; }
    public HotelTaskType TaskType { get; set; }
    public HousekeepingKind? HousekeepingKind { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public HotelTaskStatus Status { get; set; } = HotelTaskStatus.Pending;
    public int? AssignedToAccountId { get; set; }
    public int CreatedByAccountId { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByAccountId { get; set; }
    public string? CompletionNotes { get; set; }

    public Room Room { get; set; } = default!;
    public ICollection<RoomBlock> RoomBlocks { get; set; } = new List<RoomBlock>();
}

public class RoomBlock : AuditableEntity
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int? HotelTaskId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string Reason { get; set; } = default!;
    public int CreatedByAccountId { get; set; }

    public Room Room { get; set; } = default!;
    public HotelTask? HotelTask { get; set; }
}