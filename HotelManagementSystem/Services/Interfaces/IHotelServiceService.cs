using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.HotelServices;

namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface nghiệp vụ quản lý dịch vụ khách sạn (F08 - Hotel Services).
/// </summary>
public interface IHotelServiceService
{
    Task<HotelServiceDetailDto> CreateHotelServiceAsync(CreateHotelServiceRequest request);
    Task<PagedResult<HotelServiceDto>> GetHotelServicesAsync(HotelServiceQueryParameters parameters);
    Task<HotelServiceDetailDto?> GetHotelServiceByIdAsync(int id);
    Task<HotelServiceDetailDto?> UpdateHotelServiceAsync(int id, UpdateHotelServiceRequest request);
    Task<DeleteHotelServiceResultDto?> DeleteOrDeactivateHotelServiceAsync(int id);
}
