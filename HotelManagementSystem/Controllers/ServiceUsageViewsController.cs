
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-02 - View Service Usages
// Technology: ASP.NET Core OData 8.2.7
//
// DEMO LINKS:
//
// 1. ALL SERVICE USAGES:
// https://localhost:7024/odata/ServiceUsageViews
// -> Xem danh sách lượt sử dụng dịch vụ.
//
// 2. FILTER BY STAY:
// https://localhost:7024/odata/ServiceUsageViews?$filter=StayId eq 20
// -> Xem dịch vụ của Stay ID 20.
//
// 3. FILTER BY SERVICE:
// https://localhost:7024/odata/ServiceUsageViews?$filter=HotelServiceId eq 1
// -> Lọc lượt sử dụng dịch vụ ăn sáng.
//
// 4. ORDER + TOP:
// https://localhost:7024/odata/ServiceUsageViews?$orderby=Id desc&$top=5
// -> Lấy 5 lượt sử dụng có ID lớn nhất.
//
// 5. PAGINATION:
// https://localhost:7024/odata/ServiceUsageViews?$orderby=Id desc&$skip=5&$top=5
// -> Bỏ qua 5 bản ghi, lấy 5 bản ghi tiếp theo.
//
// 6. COUNT:
// https://localhost:7024/odata/ServiceUsageViews?$count=true
// -> Tổng số bản ghi chưa bị xóa mềm.
//
// 7. SELECT:
// https://localhost:7024/odata/ServiceUsageViews?$select=Id,StayId,ServiceName,Amount&$top=5
// -> Chỉ trả về các trường được chọn.
//
// NOTE:
// - OData chỉ đọc dữ liệu.
// - Không lấy ServiceUsage đã soft-delete.
// - Dùng giá snapshot từ ServiceUsage.
// =====================================================

public class ServiceUsageViewsController : ODataController
{
    private readonly ServiceUsageService _service;

    public ServiceUsageViewsController(
        ServiceUsageService service)
    {
        _service = service;
    }

    // NhaNTCE181298 | UC-F09-02 | OData
    // Hỗ trợ filter, orderby, select, count và phân trang.
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<ServiceUsageListDto> Get()
    {
        return _service.GetServiceUsages();
    }
}
