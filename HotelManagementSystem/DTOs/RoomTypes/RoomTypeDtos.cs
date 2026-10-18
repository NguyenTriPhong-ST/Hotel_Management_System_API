namespace HotelManagementSystem.DTOs.RoomTypes;

public sealed record RoomTypeDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    int Capacity,
    decimal BasePricePerNight,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string RowVersion);

public sealed record RoomTypePagedDto(
    IReadOnlyList<RoomTypeDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
