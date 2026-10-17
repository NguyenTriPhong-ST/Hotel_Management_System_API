using HotelManagementSystem.DTOs.HotelServices;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;

/// <summary>
/// Chuyển đổi dữ liệu (Mapping) giữa Entity HotelService và các DTO tương ứng.
/// </summary>
public static class HotelServiceMapper
{
    public static HotelService ToEntity(CreateHotelServiceRequest request)
    {
        return new HotelService
        {
            ServiceCode = request.ServiceCode.Trim().ToUpper(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Unit = request.Unit.Trim(),
            CurrentPrice = request.CurrentPrice,
            IsActive = request.IsActive
        };
    }

    public static void UpdateEntity(HotelService service, UpdateHotelServiceRequest request)
    {
        service.ServiceCode = request.ServiceCode.Trim().ToUpper();
        service.Name = request.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        service.Unit = request.Unit.Trim();
        service.CurrentPrice = request.CurrentPrice;
        service.IsActive = request.IsActive;
    }

    public static HotelServiceDto ToDto(HotelService service)
    {
        return new HotelServiceDto
        {
            Id = service.Id,
            ServiceCode = service.ServiceCode,
            Name = service.Name,
            Description = service.Description,
            Unit = service.Unit,
            CurrentPrice = service.CurrentPrice,
            IsActive = service.IsActive,
            TotalUsagesCount = service.ServiceUsages?.Count ?? 0,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }

    public static HotelServiceDetailDto ToDetailDto(HotelService service)
    {
        var validUsages = service.ServiceUsages?.Where(u => u.DeletedAt == null).ToList() ?? new();

        return new HotelServiceDetailDto
        {
            Id = service.Id,
            ServiceCode = service.ServiceCode,
            Name = service.Name,
            Description = service.Description,
            Unit = service.Unit,
            CurrentPrice = service.CurrentPrice,
            IsActive = service.IsActive,
            TotalUsagesCount = service.ServiceUsages?.Count ?? 0,
            TotalQuantityUsed = validUsages.Sum(u => u.Quantity),
            TotalRevenueGenerated = validUsages.Sum(u => Math.Round(u.Quantity * u.UnitPrice, 2)),
            LastUsedAt = validUsages.OrderByDescending(u => u.UsedAt).Select(u => (DateTime?)u.UsedAt).FirstOrDefault(),
            CanBePermanentlyDeleted = service.ServiceUsages == null || !service.ServiceUsages.Any(),
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }
}
