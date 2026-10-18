using HotelManagementSystem.DTOs.RoomTypes;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;

public static class RoomTypeMapper
{
    public static RoomTypeDto ToDto(RoomType x) => new(
        x.Id, x.Code, x.Name, x.Description, x.Capacity,
        x.BasePricePerNight, x.CreatedAt, x.UpdatedAt,
        Convert.ToBase64String(x.RowVersion));

    public static object ToAuditSnapshot(RoomType x) => new
    {
        x.Id,
        x.Code,
        x.Name,
        x.Description,
        x.Capacity,
        x.BasePricePerNight,
        x.DeletedAt
    };
}
