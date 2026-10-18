
using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Swagger: https://localhost:7024/swagger
//
// UC-F11-01: GET /HotelOps/reports/rooms
// UC-F11-02: GET /HotelOps/reports/reservations
// UC-F11-03: GET /HotelOps/reports/revenue
// UC-F11-04: GET /HotelOps/reports/tasks
//
// Header: X-Demo-Account-Id (Admin)
// =====================================================

[ApiController]
[Route("HotelOps/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;
    private readonly IWebHostEnvironment _environment;

    public ReportsController(
        ReportService service,
        IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    // NhaNT | F11-01 đến F11-04
    // Kiểm tra quyền Admin trong môi trường Development.
    private async Task<IActionResult?> CheckAdminAsync(
        int? accountId)
    {
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

        var account = await _service.GetAccountAsync(
            accountId.Value);

        if (account == null ||
            !account.IsActive ||
            account.Role != AccountRole.Admin)
        {
            return StatusCode(403, new
            {
                message = "Chỉ Admin được xem báo cáo."
            });
        }

        return null;
    }

    // NhaNT | F11-01 đến F11-04
    // Kiểm tra khoảng thời gian báo cáo.
    private IActionResult? ValidatePeriod(
        ReportPeriodQuery query)
    {
        if (query.FromDate == default ||
            query.ToDate == default)
        {
            return BadRequest(new
            {
                message = "Phải nhập fromDate và toDate."
            });
        }

        if (query.FromDate > query.ToDate)
        {
            return BadRequest(new
            {
                message = "fromDate không được lớn hơn toDate."
            });
        }

        if (query.ToDate == DateOnly.MaxValue)
        {
            return BadRequest(new
            {
                message = "toDate vượt giới hạn hỗ trợ."
            });
        }

        return null;
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-01: Room Report
    // GET /HotelOps/reports/rooms
    // Header: X-Demo-Account-Id
    // Query: fromDate, toDate, roomTypeId
    // =================================================
    [HttpGet("rooms")]
    [ProducesResponseType(typeof(RoomReportDto), 200)]
    public async Task<IActionResult> Rooms(
        [FromQuery] RoomReportQuery query,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? accountId)
    {
        var auth = await CheckAdminAsync(accountId);
        if (auth != null)
            return auth;

        var error = ValidatePeriod(query);
        if (error != null)
            return error;

        if (query.RoomTypeId.HasValue &&
            query.RoomTypeId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "roomTypeId phải lớn hơn 0."
            });
        }

        return Ok(await _service.GetRoomsAsync(query));
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-02: Reservation Report
    // GET /HotelOps/reports/reservations
    // Header: X-Demo-Account-Id
    // Query: fromDate, toDate, status, roomTypeId
    // =================================================
    [HttpGet("reservations")]
    [ProducesResponseType(typeof(ReservationReportDto), 200)]
    public async Task<IActionResult> Reservations(
        [FromQuery] ReservationReportQuery query,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? accountId)
    {
        var auth = await CheckAdminAsync(accountId);
        if (auth != null)
            return auth;

        var error = ValidatePeriod(query);
        if (error != null)
            return error;

        if (query.RoomTypeId.HasValue &&
            query.RoomTypeId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "roomTypeId phải lớn hơn 0."
            });
        }

        if (query.Status.HasValue &&
            !Enum.IsDefined(
                typeof(ReservationStatus),
                query.Status.Value))
        {
            return BadRequest(new
            {
                message = "ReservationStatus không hợp lệ."
            });
        }

        return Ok(await _service.GetReservationsAsync(query));
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-03: Revenue Report
    // GET /HotelOps/reports/revenue
    // Header: X-Demo-Account-Id
    // Query: fromDate, toDate, paymentMethod
    // =================================================
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), 200)]
    public async Task<IActionResult> Revenue(
        [FromQuery] RevenueReportQuery query,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? accountId)
    {
        var auth = await CheckAdminAsync(accountId);
        if (auth != null)
            return auth;

        var error = ValidatePeriod(query);
        if (error != null)
            return error;

        if (query.PaymentMethod.HasValue &&
            !Enum.IsDefined(
                typeof(PaymentMethod),
                query.PaymentMethod.Value))
        {
            return BadRequest(new
            {
                message = "PaymentMethod không hợp lệ."
            });
        }

        return Ok(await _service.GetRevenueAsync(query));
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-04: Task Report
    // GET /HotelOps/reports/tasks
    // Header: X-Demo-Account-Id
    // Query: fromDate, toDate, dateField,
    //        taskType, status, assignedToAccountId
    // =================================================
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(TaskReportDto), 200)]
    public async Task<IActionResult> Tasks(
        [FromQuery] TaskReportQuery query,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? accountId)
    {
        var auth = await CheckAdminAsync(accountId);
        if (auth != null)
            return auth;

        var error = ValidatePeriod(query);
        if (error != null)
            return error;

        if (query.DateField != "created" &&
            query.DateField != "completed")
        {
            return BadRequest(new
            {
                message = "dateField phải là created hoặc completed."
            });
        }

        if (query.TaskType.HasValue &&
            !Enum.IsDefined(
                typeof(HotelTaskType),
                query.TaskType.Value))
        {
            return BadRequest(new
            {
                message = "TaskType không hợp lệ."
            });
        }

        if (query.Status.HasValue &&
            !Enum.IsDefined(
                typeof(HotelTaskStatus),
                query.Status.Value))
        {
            return BadRequest(new
            {
                message = "TaskStatus không hợp lệ."
            });
        }

        if (query.AssignedToAccountId.HasValue &&
            query.AssignedToAccountId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "assignedToAccountId phải lớn hơn 0."
            });
        }

        return Ok(await _service.GetTasksAsync(query));
    }
}
