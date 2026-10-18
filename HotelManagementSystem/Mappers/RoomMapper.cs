using HotelManagementSystem.DTOs.Rooms;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;

namespace HotelManagementSystem.Mappers;

public static class RoomMapper
{
    // Tập trung chuyển Entity -> DTO; không truy vấn DB trong Mapper.
    public static RoomDto ToDto(RoomDataView data) => new(
        data.Room.Id,
        data.Room.RoomCode,
        data.Room.RoomTypeId,
        data.RoomTypeCode,
        data.RoomTypeName,
        data.Room.FloorNumber,
        data.Room.Description,
        (int)data.Room.OperationalStatus,
        (int)data.Room.HousekeepingStatus,
        data.IsOccupied,
        data.IsBlocked,
        data.Room.CreatedAt,
        data.Room.UpdatedAt,
        Convert.ToBase64String(data.Room.RowVersion));

    public static object ToAuditSnapshot(Room room) => new
    {
        room.Id,
        room.RoomCode,
        room.RoomTypeId,
        room.FloorNumber,
        room.Description,
        room.OperationalStatus,
        room.HousekeepingStatus,
        room.DeletedAt
    };
}
