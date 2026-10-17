using System.Security.Claims;
using HotelManagementSystem.DTOs.Auth;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers;

/// <summary>
/// Controller xử lý xác thực tài khoản và phiên đăng nhập (F01).
/// </summary>
[ApiController]
[Route("HotelOps/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// UC-F01-01 - Đăng nhập.
    /// Kiểm tra username, password và trạng thái tài khoản; trả JWT token.
    /// 
    /// API:
    /// POST https://localhost:7040/HotelOps/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F01-02 - Đăng xuất.
    /// Xử lý phiên/token theo cơ chế nhóm thống nhất; thu hồi phiên làm việc hiện tại của token.
    /// 
    /// API:
    /// POST https://localhost:7040/HotelOps/auth/logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionIdClaim = User.FindFirst("SessionId")?.Value 
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            return BadRequest(new { message = "Không tìm thấy thông tin phiên hợp lệ trong token." });
        }

        await _authService.LogoutAsync(sessionId, cancellationToken);
        return Ok(new { message = "Đăng xuất thành công. Phiên làm việc đã được thu hồi." });
    }

    /// <summary>
    /// Lấy thông tin tài khoản đang đăng nhập hiện tại từ token.
    /// 
    /// API:
    /// GET https://localhost:7040/HotelOps/auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
        {
            return Unauthorized(new { message = "Không tìm thấy định danh tài khoản hợp lệ trong token." });
        }

        try
        {
            var account = await _authService.GetCurrentAccountAsync(accountId, cancellationToken);
            return Ok(account);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
