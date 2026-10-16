
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
// Developer: NhaNTCE181298
// UC-F07-02 | OData 8.2.7
// Namespace chính xác của ODataController.
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;


namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-02 - View Stays
// Technology: ASP.NET Core OData 8.2.7
//
// ODATA DEMO LINKS
//
// 1. All Stays:
// https://localhost:7024/odata/StayViews
// -> Xem danh sách lưu trú.
//
// 2. Filter Active:
// https://localhost:7024/odata/StayViews?$filter=Status eq 'Active'
// -> Chỉ lấy Stay đang hoạt động.
//
// 3. Order + Top:
// https://localhost:7024/odata/StayViews?$orderby=Id desc&$top=5
// -> Lấy 5 Stay có ID lớn nhất.
//
// 4. Pagination:
// https://localhost:7024/odata/StayViews?$orderby=Id desc&$skip=5&$top=5
// -> Bỏ qua 5 bản ghi, lấy 5 bản ghi tiếp theo.
//
// 5. Count:
// https://localhost:7024/odata/StayViews?$count=true
// -> Trả thêm @odata.count.
//
// 6. Select:
// https://localhost:7024/odata/StayViews?$select=Id,StayCode,Status&$top=3
// -> Chỉ lấy những trường được chọn.
//
// 7. OData Metadata:
// https://localhost:7024/odata/$metadata
// -> Xem EDM Model của API.
//
// NOTE: OData chỉ đọc, không thay đổi dữ liệu.
// =====================================================

public class StayViewsController : ODataController
{
    private readonly StayService _stayService;

    public StayViewsController(StayService stayService)
    {
        _stayService = stayService;
    }

    // NhaNTCE181298 | UC-F07-02 | OData
    // Trả IQueryable để OData áp dụng truy vấn.
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<StayListDto> Get()
    {
        return _stayService.GetStays();
    }
}
