using HotelManagementSystem.DTOs.Guests;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Repositories.Interfaces;

/// <summary>
/// Interface định nghĩa các thao tác truy vấn dữ liệu cho Guest.
/// </summary>
public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(int id, bool asNoTracking = false, bool includeHistory = false);
    Task<(List<Guest> Items, int TotalCount)> GetPagedAsync(GuestQueryParameters parameters);
    Task<bool> IsDocumentExistsAsync(DocumentType? type, string? countryCode, string? number, int? excludeId = null);
    Task<bool> HasActiveReservationsAsync(int guestId);
    Task<bool> IsCurrentlyStayingAsync(int guestId);
    Task AddAsync(Guest guest);
    Task UpdateAsync(Guest guest);
    Task SaveChangesAsync();
}
