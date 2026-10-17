
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNT
// UC-F07-02: View Stays
// UC-F07-07: Search & Filter Stays
//
// ODATA - COPY LINK TEST TRỰC TIẾP:
//
// Danh sách:
// http://localhost:5142/odata/StayViews?$top=10
//
// Stay đang hoạt động:
// http://localhost:5142/odata/StayViews?$filter=Status%20eq%20'Active'
//
// Stay đã check-out:
// http://localhost:5142/odata/StayViews?$filter=Status%20eq%20'Completed'
//
// Tìm theo tên khách:
// http://localhost:5142/odata/StayViews?$filter=contains(MainGuestName,'Khang')
//
// Tìm theo mã phòng:
// http://localhost:5142/odata/StayViews?$filter=RoomCode%20eq%20'P103'
//
// Sắp xếp mới nhất:
// http://localhost:5142/odata/StayViews?$orderby=Id%20desc&$top=5
//
// Phân trang:
// http://localhost:5142/odata/StayViews?$skip=0&$top=10&$count=true
//
// Chọn trường:
// http://localhost:5142/odata/StayViews?$select=Id,StayCode,Status
//
// Metadata:
// http://localhost:5142/odata/$metadata
//
// Chỉ đọc dữ liệu, không thay đổi Database.
// =====================================================

public class StayViewsController : ODataController
{
    private readonly StayService _stayService;

    public StayViewsController(StayService stayService)
    {
        _stayService = stayService;
    }

    // NhaNT | UC-F07-02, UC-F07-07
    // GET /odata/StayViews
    // Trả IQueryable để OData lọc tại Database.
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<StayListDto> Get()
    {
        return _stayService.GetStays();
    }
}
