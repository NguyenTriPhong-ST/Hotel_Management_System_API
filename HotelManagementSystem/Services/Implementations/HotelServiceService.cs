using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.HotelServices;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai nghiệp vụ cho Feature Quản lý dịch vụ khách sạn (F08 - Hotel Services).
/// </summary>
public class HotelServiceService : IHotelServiceService
{
    private readonly IHotelServiceRepository _hotelServiceRepository;

    public HotelServiceService(IHotelServiceRepository hotelServiceRepository)
    {
        _hotelServiceRepository = hotelServiceRepository;
    }

    /// <summary>
    /// UC-F08-01 - Thêm dịch vụ khách sạn.
    /// </summary>
    public async Task<HotelServiceDetailDto> CreateHotelServiceAsync(CreateHotelServiceRequest request)
    {
        var serviceCode = request.ServiceCode.Trim().ToUpper();

        // 1. Kiểm tra chống trùng mã dịch vụ
        if (await _hotelServiceRepository.IsServiceCodeExistsAsync(serviceCode))
        {
            throw new InvalidOperationException($"Mã dịch vụ '{serviceCode}' đã tồn tại trong hệ thống.");
        }

        var hotelService = HotelServiceMapper.ToEntity(request);
        await _hotelServiceRepository.AddAsync(hotelService);
        await _hotelServiceRepository.SaveChangesAsync();

        return HotelServiceMapper.ToDetailDto(hotelService);
    }

    /// <summary>
    /// UC-F08-02 &amp; UC-F08-06 - Xem danh sách, tìm kiếm và lọc dịch vụ khách sạn.
    /// </summary>
    public async Task<PagedResult<HotelServiceDto>> GetHotelServicesAsync(HotelServiceQueryParameters parameters)
    {
        var (items, totalCount) = await _hotelServiceRepository.GetPagedAsync(parameters);

        var pageIndex = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var dtos = items.Select(HotelServiceMapper.ToDto).ToList();
        return new PagedResult<HotelServiceDto>(dtos, totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// UC-F08-03 - Xem chi tiết dịch vụ.
    /// </summary>
    public async Task<HotelServiceDetailDto?> GetHotelServiceByIdAsync(int id)
    {
        var hotelService = await _hotelServiceRepository.GetByIdAsync(id, asNoTracking: true, includeUsages: true);
        if (hotelService == null) return null;

        return HotelServiceMapper.ToDetailDto(hotelService);
    }

    /// <summary>
    /// UC-F08-04 - Cập nhật dịch vụ.
    /// </summary>
    public async Task<HotelServiceDetailDto?> UpdateHotelServiceAsync(int id, UpdateHotelServiceRequest request)
    {
        var hotelService = await _hotelServiceRepository.GetByIdAsync(id, asNoTracking: false, includeUsages: true);
        if (hotelService == null) return null;

        var serviceCode = request.ServiceCode.Trim().ToUpper();

        // Kiểm tra chống trùng mã dịch vụ với dịch vụ khác
        if (await _hotelServiceRepository.IsServiceCodeExistsAsync(serviceCode, excludeId: id))
        {
            throw new InvalidOperationException($"Mã dịch vụ '{serviceCode}' đã được sử dụng bởi một dịch vụ khác.");
        }

        HotelServiceMapper.UpdateEntity(hotelService, request);
        await _hotelServiceRepository.UpdateAsync(hotelService);
        await _hotelServiceRepository.SaveChangesAsync();

        return HotelServiceMapper.ToDetailDto(hotelService);
    }

    /// <summary>
    /// UC-F08-05 - Xóa hoặc ngừng cung cấp dịch vụ.
    /// </summary>
    public async Task<DeleteHotelServiceResultDto?> DeleteOrDeactivateHotelServiceAsync(int id)
    {
        var hotelService = await _hotelServiceRepository.GetByIdAsync(id, asNoTracking: false, includeUsages: true);
        if (hotelService == null) return null;

        var hasUsages = await _hotelServiceRepository.HasUsagesAsync(id);

        if (hasUsages)
        {
            // Đã có lịch sử sử dụng: Ngừng cung cấp để bảo toàn hóa đơn
            hotelService.IsActive = false;
            await _hotelServiceRepository.UpdateAsync(hotelService);
            await _hotelServiceRepository.SaveChangesAsync();

            return new DeleteHotelServiceResultDto
            {
                Id = hotelService.Id,
                ServiceCode = hotelService.ServiceCode,
                Name = hotelService.Name,
                IsPermanentlyDeleted = false,
                Message = $"Dịch vụ '{hotelService.Name}' đã phát sinh lượt sử dụng trong lịch sử nên không thể xóa hoàn toàn. Hệ thống đã tự động chuyển trạng thái sang ngừng cung cấp (IsActive = false) để bảo toàn dữ liệu hóa đơn."
            };
        }
        else
        {
            // Chưa từng sử dụng: Xóa vật lý
            await _hotelServiceRepository.DeleteAsync(hotelService);
            await _hotelServiceRepository.SaveChangesAsync();

            return new DeleteHotelServiceResultDto
            {
                Id = hotelService.Id,
                ServiceCode = hotelService.ServiceCode,
                Name = hotelService.Name,
                IsPermanentlyDeleted = true,
                Message = $"Dịch vụ '{hotelService.Name}' chưa từng phát sinh lượt sử dụng nào và đã được xóa hoàn toàn khỏi cơ sở dữ liệu."
            };
        }
    }
}
