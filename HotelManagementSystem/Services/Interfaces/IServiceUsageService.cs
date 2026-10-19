using HotelManagementSystem.DTOs.ServiceUsages;

namespace HotelManagementSystem.Services.Interfaces;

// Developer: NhaNTCE181298
// F09 - Service Usage Management
public interface IServiceUsageService
{
    Task<CreateServiceUsageResponse> CreateAsync(
        CreateServiceUsageRequest request,
        int accountId);

    IQueryable<ServiceUsageListDto> GetServiceUsages();

    Task<ServiceUsageDetailDto?> GetDetailAsync(int id);

    Task<CreateServiceUsageResponse> UpdateAsync(
        int id,
        UpdateServiceUsageRequest request,
        int accountId);

    Task DeleteAsync(int id, int accountId);
}