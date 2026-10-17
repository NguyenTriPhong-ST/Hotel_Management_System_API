namespace HotelManagementSystem.Features.Rooms;

public sealed record RoomDto(
    int Id,
    string RoomCode,
    int RoomTypeId,
    string RoomTypeCode,
    string RoomTypeName,
    short FloorNumber,
    string? Description,
    int OperationalStatus,
    int HousekeepingStatus,
    bool IsOccupied,
    bool IsBlocked,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string RowVersion);

public sealed record RoomPagedDto(
    IReadOnlyList<RoomDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
