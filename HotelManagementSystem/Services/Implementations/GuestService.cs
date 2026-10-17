using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.Guests;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai nghiệp vụ cho Feature Quản lý khách hàng (F05 - Guests).
/// </summary>
public class GuestService : IGuestService
{
    private readonly IGuestRepository _guestRepository;

    public GuestService(IGuestRepository guestRepository)
    {
        _guestRepository = guestRepository;
    }

    /// <summary>
    /// UC-F05-01 - Tạo hồ sơ khách.
    /// </summary>
    public async Task<GuestDetailDto> CreateGuestAsync(CreateGuestRequest request)
    {
        // Kiểm tra chống trùng giấy tờ định danh
        if (!string.IsNullOrWhiteSpace(request.DocumentNumber) && request.DocumentType.HasValue)
        {
            var isDuplicate = await _guestRepository.IsDocumentExistsAsync(
                request.DocumentType, request.DocumentCountryCode, request.DocumentNumber);

            if (isDuplicate)
            {
                throw new InvalidOperationException(
                    $"Giấy tờ định danh loại '{request.DocumentType}' số '{request.DocumentNumber}' thuộc quốc gia '{request.DocumentCountryCode}' đã tồn tại trong hệ thống.");
            }
        }

        var guest = GuestMapper.ToEntity(request);
        await _guestRepository.AddAsync(guest);
        await _guestRepository.SaveChangesAsync();

        return GuestMapper.ToDetailDto(guest);
    }

    /// <summary>
    /// UC-F05-02 &amp; UC-F05-06 - Xem danh sách, tìm kiếm và lọc hồ sơ khách hàng.
    /// </summary>
    public async Task<PagedResult<GuestDto>> GetGuestsAsync(GuestQueryParameters parameters)
    {
        var (items, totalCount) = await _guestRepository.GetPagedAsync(parameters);

        var pageIndex = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var dtos = items.Select(GuestMapper.ToDto).ToList();
        return new PagedResult<GuestDto>(dtos, totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// UC-F05-03 - Xem chi tiết khách.
    /// </summary>
    public async Task<GuestDetailDto?> GetGuestByIdAsync(int id)
    {
        var guest = await _guestRepository.GetByIdAsync(id, asNoTracking: true, includeHistory: true);
        if (guest == null) return null;

        return GuestMapper.ToDetailDto(guest);
    }

    /// <summary>
    /// UC-F05-04 - Cập nhật hồ sơ khách.
    /// </summary>
    public async Task<GuestDetailDto?> UpdateGuestAsync(int id, UpdateGuestRequest request)
    {
        var guest = await _guestRepository.GetByIdAsync(id, asNoTracking: false, includeHistory: true);
        if (guest == null) return null;

        // Kiểm tra chống trùng giấy tờ với khách khác
        if (!string.IsNullOrWhiteSpace(request.DocumentNumber) && request.DocumentType.HasValue)
        {
            var isDuplicate = await _guestRepository.IsDocumentExistsAsync(
                request.DocumentType, request.DocumentCountryCode, request.DocumentNumber, excludeId: id);

            if (isDuplicate)
            {
                throw new InvalidOperationException(
                    $"Giấy tờ định danh loại '{request.DocumentType}' số '{request.DocumentNumber}' thuộc quốc gia '{request.DocumentCountryCode}' đã được sử dụng bởi khách hàng khác.");
            }
        }

        GuestMapper.UpdateEntity(guest, request);
        await _guestRepository.UpdateAsync(guest);
        await _guestRepository.SaveChangesAsync();

        return GuestMapper.ToDetailDto(guest);
    }

    /// <summary>
    /// UC-F05-05 - Xóa hồ sơ khách.
    /// </summary>
    public async Task<bool> DeleteGuestAsync(int id)
    {
        var guest = await _guestRepository.GetByIdAsync(id, asNoTracking: false, includeHistory: false);
        if (guest == null) return false;

        // 1. Kiểm tra đặt phòng còn hiệu lực
        if (await _guestRepository.HasActiveReservationsAsync(id))
        {
            throw new InvalidOperationException(
                "Không thể xóa hồ sơ khách hàng đang có đặt phòng chưa hoàn thành hoặc đang hoạt động.");
        }

        // 2. Kiểm tra đang lưu trú
        if (await _guestRepository.IsCurrentlyStayingAsync(id))
        {
            throw new InvalidOperationException(
                "Không thể xóa hồ sơ khách hàng đang trong kỳ lưu trú tại khách sạn.");
        }

        // 3. Xóa mềm bảo toàn lịch sử
        guest.DeletedAt = DateTime.UtcNow;
        await _guestRepository.UpdateAsync(guest);
        await _guestRepository.SaveChangesAsync();

        return true;
    }
}
