using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers;

/// <summary>
/// Controller quản lý tài khoản nhân viên dành cho Quản trị viên (F02).
/// Tất cả các endpoint đều yêu cầu quyền Admin.
/// </summary>
[ApiController]
[Route("HotelOps/accounts")]
[Authorize(Roles = "Admin")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// UC-F02-01 - Tạo tài khoản nhân viên.
    /// Kiểm tra username duy nhất, role hợp lệ và lưu password hash.
    /// 
    /// API:
    /// POST https://localhost:7040/HotelOps/accounts
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _accountService.CreateAccountAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F02-02 &amp; UC-F02-07 - Xem danh sách, tìm kiếm và lọc tài khoản.
    /// Hỗ trợ tìm theo keyword username/tên, lọc theo role, trạng thái, phân trang và sắp xếp.
    /// 
    /// API:
    /// GET https://localhost:7040/HotelOps/accounts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts([FromQuery] AccountFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAccountsAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// UC-F02-03 - Xem chi tiết tài khoản.
    /// Truy vấn theo ID; trả thông tin, role và trạng thái.
    /// 
    /// API:
    /// GET https://localhost:7040/HotelOps/accounts/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAccountById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _accountService.GetAccountByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F02-04 - Cập nhật thông tin tài khoản.
    /// Validate dữ liệu cập nhật và chống trùng lặp username.
    /// 
    /// API:
    /// PUT https://localhost:7040/HotelOps/accounts/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _accountService.UpdateAccountAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F02-05 - Thay đổi role tài khoản.
    /// Role được gán phải thuộc các role của hệ thống; thu hồi phiên cũ để đồng bộ quyền mới.
    /// 
    /// API:
    /// PATCH https://localhost:7040/HotelOps/accounts/{id}/role
    /// </summary>
    [HttpPatch("{id:int}/role")]
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _accountService.ChangeRoleAsync(id, request.Role, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F02-06 - Kích hoạt hoặc vô hiệu hóa tài khoản.
    /// Cập nhật trạng thái; xử lý thu hồi quyền truy cập nếu tài khoản bị khóa.
    /// 
    /// API:
    /// PATCH https://localhost:7040/HotelOps/accounts/{id}/status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateStatusRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _accountService.ChangeStatusAsync(id, request.IsActive, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
