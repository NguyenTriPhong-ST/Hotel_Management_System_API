
using HotelManagementSystem.DTOs.Invoices;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// Module: F07 - Invoice Management
// UC-F07-04: Calculate & Finalize Invoice
//
// Swagger: https://localhost:7024/swagger
// POST /HotelOps/stays/{stayId}/invoice/calculate
// POST /HotelOps/stays/{stayId}/invoice/finalize
// Header: X-Demo-Account-Id
// =====================================================

[ApiController]
[Route("HotelOps/stays/{stayId:int}/invoice")]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _service;
    private readonly IWebHostEnvironment _environment;

    public InvoicesController(
        InvoiceService service,
        IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F07-04: Calculate Invoice
    // POST /HotelOps/stays/{stayId}/invoice/calculate
    // =================================================
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(InvoiceResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Calculate(
        int stayId,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "API đang chờ tích hợp JWT."
            });
        }

        if (!demoAccountId.HasValue ||
            demoAccountId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _service.CalculateAsync(
                stayId,
                demoAccountId.Value);

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
                message = "Dữ liệu đã thay đổi, vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột dữ liệu khi tính hóa đơn."
            });
        }
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F07-04: Finalize Invoice
    // POST /HotelOps/stays/{stayId}/invoice/finalize
    // =================================================
    [HttpPost("finalize")]
    [ProducesResponseType(typeof(InvoiceResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Finalize(
        int stayId,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "API đang chờ tích hợp JWT."
            });
        }

        if (!demoAccountId.HasValue ||
            demoAccountId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _service.FinalizeAsync(
                stayId,
                demoAccountId.Value);

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
                message = "Dữ liệu đã thay đổi, vui lòng thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột dữ liệu khi chốt hóa đơn."
            });
        }
    }
}
