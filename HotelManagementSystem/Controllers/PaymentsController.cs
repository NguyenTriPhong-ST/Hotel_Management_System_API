
using HotelManagementSystem.DTOs.Payments;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Record Internal Payment
//
// SWAGGER: https://localhost:7024/swagger
// Method: POST
// Endpoint: /api/invoices/{invoiceId}/payments
// Header: X-Demo-Account-Id
// Body: RecordPaymentRequest
//
// 201: Thanh toán thành công.
// 200: Gửi lại giao dịch đã tồn tại.
// 400: Dữ liệu đầu vào không hợp lệ.
// 403: Không có quyền thanh toán.
// 404: Không tìm thấy Invoice.
// 409: Xung đột hoặc vượt số tiền còn lại.
//
// OData: Không sử dụng.
// =====================================================

[ApiController]
[Route("api/invoices/{invoiceId:int}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _service;
    private readonly IWebHostEnvironment _environment;

    public PaymentsController(
        PaymentService service,
        IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResultDto), 201)]
    [ProducesResponseType(typeof(PaymentResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Record(
        int invoiceId,
        [FromBody] RecordPaymentRequest request,
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
            var result = await _service.RecordAsync(
                invoiceId, request, accountId.Value);

            if (result.Message ==
                "Giao dịch đã được ghi nhận trước đó.")
                return Ok(result);

            return StatusCode(201, result);
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
                message = "Dữ liệu đã thay đổi, thử lại."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Xung đột thanh toán hoặc IdempotencyKey."
            });
        }
    }
}
