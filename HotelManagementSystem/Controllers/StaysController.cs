
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// Module: F07 - Stay Management
// Swagger: https://localhost:7024/swagger
//
// UC-F07-01: POST /HotelOps/stays/check-in
// UC-F07-02: GET  /HotelOps/stays
// UC-F07-03: GET  /HotelOps/stays/{id}
// UC-F07-06: POST /HotelOps/stays/{id}/check-out
// UC-F07-07: GET  /HotelOps/stays (search/filter)
// =====================================================

[ApiController]
[Route("HotelOps/stays")]
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
    // Developer: NhaNTCE181298
    // UC-F07-01: Check-in
    // POST /HotelOps/stays/check-in
    // Header: X-Demo-Account-Id
    // Body: CheckInRequest
    // =================================================
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "Chưa tích hợp JWT Authentication F01."
            });
        }

        if (!demoAccountId.HasValue || demoAccountId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _stayService.CheckInAsync(
                request, demoAccountId.Value);

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
                message = "Dữ liệu đã thay đổi, vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột dữ liệu khi check-in."
            });
        }
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F07-02: View Stays
    // UC-F07-07: Search & Filter Stays
    // GET /HotelOps/stays
    //
    // Query: search, status, page, pageSize, sort
    //
    // Ví dụ:
    // GET /HotelOps/stays?page=1&pageSize=20
    // GET /HotelOps/stays?status=Active
    // GET /HotelOps/stays?search=ST-001
    // GET /HotelOps/stays?sort=oldest
    // =================================================
    [HttpGet]
    public async Task<IActionResult> GetStays(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "newest")
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                message = "page phải lớn hơn hoặc bằng 1."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "pageSize phải từ 1 đến 100."
            });
        }

        var query = _stayService.GetStays();

        // NhaNT | F07-07 - Tìm mã Stay, phòng hoặc tên khách.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(s =>
                s.StayCode.Contains(keyword) ||
                s.ReservationCode.Contains(keyword) ||
                s.RoomCode.Contains(keyword) ||
                s.MainGuestName.Contains(keyword));
        }

        // NhaNT | F07-07 - Lọc trạng thái Stay.
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();

            if (!normalizedStatus.Equals(
                    "Active", StringComparison.OrdinalIgnoreCase) &&
                !normalizedStatus.Equals(
                    "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "status chỉ nhận Active hoặc Completed."
                });
            }

            var expectedStatus =
                normalizedStatus.Equals(
                    "Active", StringComparison.OrdinalIgnoreCase)
                    ? "Active"
                    : "Completed";

            query = query.Where(s => s.Status == expectedStatus);
        }

        // NhaNT | F07-07 - Sắp xếp có thứ tự ổn định.
        var normalizedSort = sort.Trim().ToLowerInvariant();

        query = normalizedSort switch
        {
            "newest" => query.OrderByDescending(s => s.Id),
            "oldest" => query.OrderBy(s => s.Id),
            _ => query.OrderByDescending(s => s.Id)
        };

        if (normalizedSort != "newest" &&
            normalizedSort != "oldest")
        {
            return BadRequest(new
            {
                message = "sort chỉ nhận newest hoặc oldest."
            });
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items,
            totalItems,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        });
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F07-03: View Stay Detail
    // GET /HotelOps/stays/{id}
    // Input: id của Stay.
    // =================================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStayDetail(int id)
    {
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
    // Developer: NhaNTCE181298
    // UC-F07-06: Check-out
    // POST /HotelOps/stays/{id}/check-out
    // Header: X-Demo-Account-Id
    // Điều kiện: Invoice Finalized và thanh toán đủ.
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
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "Đang chờ tích hợp JWT."
            });
        }

        if (!accountId.HasValue || accountId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _stayService.CheckOutAsync(
                id, accountId.Value);

            return Ok(result);
        }
        catch (StayBusinessException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
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
                message = "Dữ liệu đã thay đổi, vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột dữ liệu khi check-out."
            });
        }
    }
}
