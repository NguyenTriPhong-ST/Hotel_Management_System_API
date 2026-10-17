using HotelManagementSystem.DTOs.HotelServices;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

/// <summary>
/// Interface định nghĩa các thao tác truy vấn dữ liệu cho HotelService.
/// </summary>
public interface IHotelServiceRepository
{
    Task<HotelService?> GetByIdAsync(int id, bool asNoTracking = false, bool includeUsages = false);
    Task<(List<HotelService> Items, int TotalCount)> GetPagedAsync(HotelServiceQueryParameters parameters);
    Task<bool> IsServiceCodeExistsAsync(string serviceCode, int? excludeId = null);
    Task<bool> HasUsagesAsync(int id);
    Task AddAsync(HotelService service);
    Task UpdateAsync(HotelService service);
    Task DeleteAsync(HotelService service);
    Task SaveChangesAsync();
}
