using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.Guests;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers;

/// <summary>
/// Feature F05 - Quản lý hồ sơ khách hàng (Guests).
/// Đối tượng sử dụng: Receptionist.
/// </summary>
[ApiController]
[Route("HotelOps/guests")]
[Produces("application/json")]
public class GuestsController : ControllerBase
{
    private readonly IGuestService _guestService;

    public GuestsController(IGuestService guestService)
    {
        _guestService = guestService;
    }

    /// <summary>
    /// UC-F05-01 - Tạo hồ sơ khách.
    /// Kiểm tra thông tin bắt buộc, nhóm giấy tờ định danh và tạo mới một Guest.
    /// 
    /// API:
    /// POST https://localhost:7024/HotelOps/guests
    /// </summary>
    /// <param name="request">Dữ liệu tạo mới hồ sơ khách hàng.</param>
    /// <returns>Hồ sơ khách hàng vừa được tạo thành công.</returns>
    /// <response code="201">Tạo hồ sơ thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc số giấy tờ đã tồn tại trong hệ thống.</response>
    [HttpPost]
    [ProducesResponseType(typeof(GuestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGuest([FromBody] CreateGuestRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var createdGuest = await _guestService.CreateGuestAsync(request);
            return CreatedAtAction(nameof(GetGuestById), new { id = createdGuest.Id }, createdGuest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F05-02 &amp; UC-F05-06 - Xem danh sách, tìm kiếm và lọc khách hàng.
    /// Hỗ trợ tìm kiếm theo từ khóa (tên, SĐT, email, số giấy tờ), lọc theo lịch sử lưu trú và phân trang.
    /// 
    /// API:
    /// GET https://localhost:7024/HotelOps/guests
    /// </summary>
    /// <param name="parameters">Các tiêu chí lọc, tìm kiếm, sắp xếp và phân trang.</param>
    /// <returns>Danh sách khách hàng phân trang thỏa mãn điều kiện.</returns>
    /// <response code="200">Trả về danh sách khách hàng thành công.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GuestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuests([FromQuery] GuestQueryParameters parameters)
    {
        var result = await _guestService.GetGuestsAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// UC-F05-03 - Xem chi tiết khách.
    /// Truy vấn theo ID, trả về thông tin hồ sơ chi tiết cùng lịch sử đặt phòng và lịch sử lưu trú.
    /// 
    /// API:
    /// GET https://localhost:7024/HotelOps/guests/{id}
    /// </summary>
    /// <param name="id">Mã định danh khách hàng.</param>
    /// <returns>Thông tin hồ sơ chi tiết kèm lịch sử liên quan.</returns>
    /// <response code="200">Tìm thấy khách hàng.</response>
    /// <response code="404">Không tìm thấy khách hàng với mã tương ứng.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GuestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuestById([FromRoute] int id)
    {
        var guest = await _guestService.GetGuestByIdAsync(id);
        if (guest == null)
        {
            return NotFound(new { message = $"Không tìm thấy khách hàng với ID = {id}." });
        }

        return Ok(guest);
    }

    /// <summary>
    /// UC-F05-04 - Cập nhật hồ sơ khách.
    /// Validate dữ liệu, chống trùng giấy tờ định danh và cập nhật thông tin khách hàng.
    /// 
    /// API:
    /// PUT https://localhost:7024/HotelOps/guests/{id}
    /// </summary>
    /// <param name="id">Mã định danh khách hàng cần chỉnh sửa.</param>
    /// <param name="request">Thông tin cập nhật mới.</param>
    /// <returns>Hồ sơ khách hàng sau khi cập nhật.</returns>
    /// <response code="200">Cập nhật hồ sơ thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc số giấy tờ bị trùng với khách khác.</response>
    /// <response code="404">Không tìm thấy khách hàng cần sửa.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GuestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGuest([FromRoute] int id, [FromBody] UpdateGuestRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updatedGuest = await _guestService.UpdateGuestAsync(id, request);
            if (updatedGuest == null)
            {
                return NotFound(new { message = $"Không tìm thấy khách hàng với ID = {id}." });
            }

            return Ok(updatedGuest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F05-05 - Xóa hồ sơ khách.
    /// Áp dụng quy tắc bảo toàn lịch sử: Từ chối xóa nếu khách đang có đặt phòng còn hiệu lực hoặc đang lưu trú.
    /// Thực hiện xóa mềm (DeletedAt) khi đủ điều kiện.
    /// 
    /// API:
    /// DELETE https://localhost:7024/HotelOps/guests/{id}
    /// </summary>
    /// <param name="id">Mã định danh khách hàng cần xóa.</param>
    /// <returns>Kết quả thực hiện thao tác xóa.</returns>
    /// <response code="200">Xóa hồ sơ khách thành công.</response>
    /// <response code="400">Không thể xóa do khách đang có đặt phòng hoặc lưu trú hoạt động.</response>
    /// <response code="404">Không tìm thấy khách hàng cần xóa.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGuest([FromRoute] int id)
    {
        try
        {
            var success = await _guestService.DeleteGuestAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy khách hàng với ID = {id}." });
            }

            return Ok(new { message = $"Đã xóa thành công hồ sơ khách hàng với ID = {id}." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
