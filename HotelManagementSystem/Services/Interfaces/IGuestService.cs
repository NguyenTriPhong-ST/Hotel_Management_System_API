using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.Guests;

namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface nghiệp vụ quản lý hồ sơ khách hàng (F05 - Guests).
/// </summary>
public interface IGuestService
{
    Task<GuestDetailDto> CreateGuestAsync(CreateGuestRequest request);
    Task<PagedResult<GuestDto>> GetGuestsAsync(GuestQueryParameters parameters);
    Task<GuestDetailDto?> GetGuestByIdAsync(int id);
    Task<GuestDetailDto?> UpdateGuestAsync(int id, UpdateGuestRequest request);
    Task<bool> DeleteGuestAsync(int id);
}
