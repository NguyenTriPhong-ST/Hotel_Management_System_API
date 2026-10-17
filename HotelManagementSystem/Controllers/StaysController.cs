
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNT
// Module: F07 - Stay Management
//
// SWAGGER: https://localhost:7024/swagger
//
// UC-F07-01: Check-in
// POST /api/stays/check-in
//
// UC-F07-02: View Stays
// GET /api/stays
//
// UC-F07-03: View Stay Detail
// GET /api/stays/{id}
//
// UC-F07-06: Check-out
// POST /api/stays/{id}/check-out
//
// UC-F07-07: Search & Filter Stays
// GET /odata/StayViews
//
// Header POST: X-Demo-Account-Id
// Chỉ dùng header demo khi chưa tích hợp JWT F01.
// =====================================================

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

    // =================================================
    // Developer: NhaNT
    // UC-F07-01: Check-in
    //
    // Swagger: https://localhost:7024/swagger
    // Method: POST
    // Endpoint: /api/stays/check-in
    // Header: X-Demo-Account-Id
    // Body: CheckInRequest
    //
    // Tạo Stay, StayGuests và cập nhật Reservation.
    // =================================================
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        // NhaNT | F07-01
        // Chờ JWT F01, header demo chỉ chạy Development.
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "Chưa tích hợp JWT Authentication F01."
            });
        }

        // NhaNT | F07-01
        // Yêu cầu Account ID của Receptionist.
        if (demoAccountId == null || demoAccountId <= 0)
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

    // =================================================
    // Developer: NhaNT
    // UC-F07-02: View Stays
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/stays
    //
    // OData chuyên dụng:
    // http://localhost:5142/odata/StayViews?$top=10
    // =================================================
    [HttpGet]
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<StayListDto> GetStays()
    {
        return _stayService.GetStays();
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-03: View Stay Detail
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/stays/{id}
    // Input: id = ID của Stay
    //
    // Kết quả: Stay, khách lưu trú và dịch vụ.
    // =================================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStayDetail(int id)
    {
        // NhaNT | F07-03 - Kiểm tra ID.
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Stay ID phải lớn hơn 0."
            });
        }

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

    // =================================================
    // Developer: NhaNT
    // UC-F07-06: Check-out
    //
    // Swagger: https://localhost:7024/swagger
    // Method: POST
    // Endpoint: /api/stays/{id}/check-out
    // Header: X-Demo-Account-Id
    // Input: id = ID của Stay
    // Body: Không có
    //
    // Điều kiện: Invoice Finalized và đã thanh toán đủ.
    // Kết quả: Check-out, cập nhật trạng thái phòng.
    // =================================================
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
        // NhaNT | F07-06
        // Header demo chỉ dùng trong Development.
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "Đang chờ tích hợp JWT."
            });
        }

        if (!accountId.HasValue || accountId <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _stayService.CheckOutAsync(
                id,
                accountId.Value);

            return Ok(result);
        }
        catch (InvoiceBusinessException ex)
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
                    "Dữ liệu đã thay đổi, vui lòng thử lại."
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
