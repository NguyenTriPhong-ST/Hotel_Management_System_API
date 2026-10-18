using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

// DTO nhập từ HTTP không đi trực tiếp xuống tầng Repository.
public sealed record RoomTypeSearchOptions(
    string? Name,
    int? MinCapacity,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection);

public interface IRoomTypeRepository
{
    Task<bool> CodeExistsAsync(string code, int? excludingId = null);
    Task<RoomType?> FindActiveAsync(int id, bool tracking = false);
    Task<bool> HasActiveRoomsAsync(int roomTypeId);
    Task<(IReadOnlyList<RoomType> Items, int TotalCount)> GetPagedAsync(RoomTypeSearchOptions options);

    // Transaction: lưu RoomType để lấy Id rồi ghi AuditLog trong cùng transaction.
    Task InsertWithAuditAsync(RoomType entity, Func<RoomType, AuditLog> buildAudit);
    // Thay đổi entity tracked và audit được SaveChanges cùng một lần.
    Task SaveWithAuditAsync(RoomType entity, byte[] originalRowVersion, AuditLog audit);
}
