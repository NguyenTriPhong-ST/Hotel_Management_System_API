using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

// Các kiểu dành riêng cho Data Access; Repository không nhận HTTP Request DTO.
public sealed record RoomSearchOptions(
    string? RoomCode,
    int? RoomTypeId,
    short? FloorNumber,
    int? OperationalStatus,
    int? HousekeepingStatus,
    bool? IsOccupied,
    bool? IsBlocked,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection,
    DateTime NowUtc);

public sealed record RoomDataView(
    Room Room,
    string RoomTypeCode,
    string RoomTypeName,
    bool IsOccupied,
    bool IsBlocked);

public sealed record RoomStatusDependencies(
    bool IsOccupied,
    bool IsBlocked,
    bool MaintenanceInProgress,
    bool HousekeepingInProgress);

public interface IRoomRepository
{
    Task<bool> CodeExistsAsync(string roomCode, int? excludingId = null);
    Task<bool> ActiveRoomTypeExistsAsync(int roomTypeId);
    Task<Room?> FindActiveAsync(int id, bool tracking = false);
    Task<RoomDataView?> GetDetailAsync(int id, DateTime nowUtc);
    Task<(IReadOnlyList<RoomDataView> Items, int TotalCount)> GetPagedAsync(RoomSearchOptions options);

    Task<bool> HasReservationOrStayAsync(int roomId);
    Task<bool> HasAnyRelatedRecordsAsync(int roomId);
    Task<RoomStatusDependencies> GetStatusDependenciesAsync(int roomId, DateTime nowUtc);

    // Chèn Room lấy Id, rồi AuditLog: commit cùng một transaction.
    Task InsertWithAuditAsync(Room entity, Func<Room, AuditLog> buildAudit);
    // Một SaveChanges lưu cả thay đổi Room và AuditLog, có kiểm soát RowVersion.
    Task SaveWithAuditAsync(Room entity, byte[] originalRowVersion, AuditLog audit);
}
