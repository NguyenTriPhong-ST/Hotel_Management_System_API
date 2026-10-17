
using System.Linq.Expressions;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;

// =====================================================
// Developer: NhaNTCE181298
// UC-F09-01, 02, 04: ServiceUsage Mapper
// Chuyển Entity thành DTO, giữ nguyên giá snapshot.
//
// OData:
// https://localhost:7024/odata/ServiceUsageViews?$top=10
// =====================================================

public static class ServiceUsageMapper
{
    // NhaNT | UC-F09-02
    // Expression giúp EF Core dịch OData thành SQL.
    public static readonly Expression<
        Func<ServiceUsage, ServiceUsageListDto>>
        ToListDto = su => new ServiceUsageListDto
        {
            Id = su.Id,
            StayId = su.StayId,
            StayCode = su.Stay.StayCode,
            RoomId = su.Stay.RoomId,
            RoomCode = su.Stay.Room.RoomCode,
            HotelServiceId = su.HotelServiceId,
            ServiceName = su.ServiceNameSnapshot,
            Unit = su.UnitSnapshot,
            Quantity = su.Quantity,
            UnitPrice = su.UnitPrice,
            Amount = su.Quantity * su.UnitPrice,
            UsedAt = su.UsedAt,
            RecordedByAccountId = su.RecordedByAccountId,
            Notes = su.Notes
        };

    // NhaNT | UC-F09-01, UC-F09-04
    // Kết quả tạo hoặc cập nhật lượt sử dụng.
    public static CreateServiceUsageResponse ToResponse(
        ServiceUsage usage,
        string message)
    {
        return new CreateServiceUsageResponse
        {
            Id = usage.Id,
            StayId = usage.StayId,
            HotelServiceId = usage.HotelServiceId,
            ServiceName = usage.ServiceNameSnapshot,
            Unit = usage.UnitSnapshot,
            Quantity = usage.Quantity,
            UnitPrice = usage.UnitPrice,
            Amount = usage.Quantity * usage.UnitPrice,
            UsedAt = usage.UsedAt,
            RecordedByAccountId = usage.RecordedByAccountId,
            Notes = usage.Notes,
            Message = message
        };
    }
}
