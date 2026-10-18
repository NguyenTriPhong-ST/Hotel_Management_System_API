using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.Rooms;

public sealed class CreateRoomRequest
{
    [Required, MaxLength(20)]
    public string RoomCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int RoomTypeId { get; set; }

    // Kiểu short tương ứng cột smallint. Thiết kế DB không giới hạn tầng âm.
    public short FloorNumber { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 2)]
    public int OperationalStatus { get; set; } = 1;

    [Range(1, 4)]
    public int HousekeepingStatus { get; set; } = 1;
}

public sealed class UpdateRoomRequest
{
    [Required, MaxLength(20)]
    public string RoomCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int RoomTypeId { get; set; }

    public short FloorNumber { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class UpdateRoomStatusRequest
{
    // Cho phép cập nhật một hoặc cả hai nhóm trạng thái.
    [Range(1, 2)]
    public int? OperationalStatus { get; set; }

    [Range(1, 4)]
    public int? HousekeepingStatus { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class RoomListQuery
{
    public string? RoomCode { get; set; }

    [Range(1, int.MaxValue)]
    public int? RoomTypeId { get; set; }

    public short? FloorNumber { get; set; }

    [Range(1, 2)]
    public int? OperationalStatus { get; set; }

    [Range(1, 4)]
    public int? HousekeepingStatus { get; set; }

    public bool? IsOccupied { get; set; }

    public bool? IsBlocked { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "code";

    public string SortDirection { get; set; } = "asc";
}
