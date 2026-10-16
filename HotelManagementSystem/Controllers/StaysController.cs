
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;


namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-01 - Check-in
// Mô tả: Cung cấp API nhận phòng và trả kết quả.
// OData: Không dùng vì đây là POST.
// =====================================================


/*
 * =====================================================
 * Developer: NhaNTCE181298
 * UC: UC-F07-07 - Search & Filter Stays
 * Technology: ASP.NET Core OData 8.2.7
 * Endpoint: GET /odata/StayViews
 *
 * DEMO LINKS:
 *
 * 1. Search by StayCode:
 * https://localhost:7024/odata/StayViews?$filter=StayCode eq 'ST-001'
 * -> Tim Stay theo ma luu tru.
 *
 * 2. Search by RoomCode:
 * https://localhost:7024/odata/StayViews?$filter=RoomCode eq 'P103'
 * -> Tim Stay theo ma phong.
 *
 * 3. Search by GuestName:
 * https://localhost:7024/odata/StayViews?$filter=contains(MainGuestName,'Khang')
 * -> Tim kiem ten khach co chua "Khang".
 *
 * 4. Filter by Status:
 * https://localhost:7024/odata/StayViews?$filter=Status eq 'Active'
 * -> Loc Stay dang luu tru.
 *
 * 5. Combined Filter:
 * https://localhost:7024/odata/StayViews?$filter=Status eq 'Active' and RoomCode eq 'P103'
 * -> Loc Stay Active cua phong P103.
 *
 * NOTE:
 * - Reuse UC-F07-02 OData endpoint.
 * - No database modification.
 * - OData filters are executed through EF Core.
 * =====================================================
 */


[ApiController]
[Route("api/stays")]
public class StaysController : ControllerBase
{
    private readonly StayService _stayService;
    private readonly IWebHostEnvironment _environment;

    public StaysController(
        StayService stayService,
        IWebHostEnvironment environment)
    {
        _stayService = stayService;
        _environment = environment;
    }

    // NhaNTCE181298 | UC-F07-01
    // POST /api/stays/check-in
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        // NhaNTCE181298 | UC-F07-01
        // Header demo chỉ được phép trong Development.
        // Production phải tích hợp JWT của F01.
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message =
                    "Chưa tích hợp JWT Authentication F01."
            });
        }

        if (demoAccountId == null ||
            demoAccountId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Cần truyền X-Demo-Account-Id " +
                    "của Receptionist để kiểm thử."
            });
        }

        try
        {
            var result = await _stayService.CheckInAsync(
                request,
                demoAccountId.Value);

            return Ok(result);
        }
        catch (StayBusinessException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message =
                    "Dữ liệu đã được cập nhật bởi " +
                    "một thao tác khác. Vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message =
                    "Dữ liệu bị xung đột hoặc " +
                    "không thỏa mãn ràng buộc Database."
            });
        }
    }

    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F07-02 - View Stays
    // OData: $filter, $orderby, $top, $skip,
    // $count và $select.
    // Mô tả: Lấy danh sách Stay từ SQL Server.
    // =====================================================

    [HttpGet]
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<StayListDto> GetStays()
    {
        // NhaNTCE181298 | UC-F07-02 | OData
        // API GET chỉ đọc, không thay đổi dữ liệu.
        // OData áp dụng các tham số truy vấn lên IQueryable.
        return _stayService.GetStays();
    }

    /*
     * ============================================================
     * Developer: NhaNTCE181298
     * UC: UC-F07-02 - View Stays
     * Technology: OData 8.2.7
     * Endpoint: GET /api/stays
     *
     * ODATA TEST LINKS - SWAGGER DEMO
     * Base URL: https://localhost:7024/api/stays
     *
     * 1. GET ALL STAYS
     * https://localhost:7024/api/stays
     * -> Xem danh sách lưu trú.
     *
     * 2. FILTER ACTIVE STAYS
     * https://localhost:7024/api/stays?$filter=Status eq 'Active'
     * -> Chỉ lấy các Stay đang hoạt động.
     *
     * 3. FILTER COMPLETED STAYS
     * https://localhost:7024/api/stays?$filter=Status eq 'Completed'
     * -> Chỉ lấy các Stay đã check-out.
     *
     * 4. ORDER BY ID DESC + TOP 5
     * https://localhost:7024/api/stays?$orderby=Id desc&$top=5
     * -> Lấy 5 Stay có ID lớn nhất.
     *
     * 5. PAGINATION
     * https://localhost:7024/api/stays?$orderby=Id desc&$skip=5&$top=5
     * -> Bỏ qua 5 Stay đầu, lấy 5 Stay tiếp theo.
     *
     * 6. SELECT FIELDS
     * https://localhost:7024/api/stays?$select=Id,StayCode,Status&$top=3
     * -> Chỉ trả về ID, mã Stay và trạng thái.
     *
     * 7. COUNT STAYS
     * https://localhost:7024/api/stays?$count=true
     * -> Yêu cầu đếm tổng Stay.
     * -> TODO: Hoàn thiện phản hồi @odata.count.
     *
     * 8. FILTER BY ROOM
     * https://localhost:7024/api/stays?$filter=RoomCode eq 'P103'
     * -> Tìm Stay theo mã phòng.
     *
     * NOTE:
     * - OData chỉ áp dụng cho GET /api/stays.
     * - POST /api/stays/check-in không sử dụng OData.
     * - Các URL sử dụng địa chỉ Development trên máy local.
     * - Có thể thay localhost:7024 nếu port thay đổi.
     * ============================================================
     */




    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F07-03 - View Stay Details
    //
    // DEMO LINKS:
    // https://localhost:7024/api/stays/20
    // -> Stay đang hoạt động, vừa Check-in.
    //
    // https://localhost:7024/api/stays/1
    // -> Stay đã hoàn thành.
    //
    // https://localhost:7024/api/stays/99999
    // -> Kiểm tra lỗi 404 Not Found.
    //
    // OData: Không sử dụng cho API chi tiết.
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStayDetail(int id)
    {
        // Kiểm tra ID hợp lệ.
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Stay ID phải lớn hơn 0."
            });
        }

        // Gọi Service lấy chi tiết Stay.
        var stay = await _stayService.GetStayDetailAsync(id);

        if (stay == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Stay."
            });
        }

        return Ok(stay);
    }


    // =====================================================
    // Developer: NhaNTCE181298
    // UC-F07-06: Check-out
    // POST /api/stays/{id}/check-out
    // =====================================================

    [HttpPost("{id:int}/check-out")]
    [ProducesResponseType(typeof(CheckOutResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CheckOut(
        int id,
        [FromHeader(Name = "X-Demo-Account-Id")]
    int? accountId)
    {
        if (!_environment.IsDevelopment())
            return StatusCode(503, new
            {
                message = "Đang chờ tích hợp JWT."
            });

        if (!accountId.HasValue || accountId <= 0)
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });

        try
        {

            var result = await _stayService.CheckOutAsync(
                id, accountId.Value);


            return Ok(result);
        }
        catch (InvoiceBusinessException ex)
        {
            return StatusCode(ex.StatusCode,
                new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "Dữ liệu đã thay đổi, vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột dữ liệu check-out."
            });
        }
    }

}
