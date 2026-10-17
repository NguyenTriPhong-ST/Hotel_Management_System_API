using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.DTOs.HotelServices;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.Controllers;

/// <summary>
/// Feature F08 - Quản lý dịch vụ khách sạn (Hotel Services).
/// Đối tượng sử dụng: Admin.
/// </summary>
[ApiController]
[Route("HotelOps/services")]
[Produces("application/json")]
public class HotelServicesController : ControllerBase
{
    private readonly IHotelServiceService _hotelServiceService;

    public HotelServicesController(IHotelServiceService hotelServiceService)
    {
        _hotelServiceService = hotelServiceService;
    }

    /// <summary>
    /// UC-F08-01 - Thêm dịch vụ khách sạn.
    /// Kiểm tra tính hợp lệ của dữ liệu, chống trùng mã dịch vụ và tạo mới dịch vụ.
    /// 
    /// API:
    /// POST https://localhost:7024/HotelOps/services
    /// </summary>
    /// <param name="request">Dữ liệu tạo mới dịch vụ.</param>
    /// <returns>Chi tiết dịch vụ vừa được tạo.</returns>
    /// <response code="201">Tạo dịch vụ thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc mã dịch vụ đã tồn tại.</response>
    [HttpPost]
    [ProducesResponseType(typeof(HotelServiceDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateService([FromBody] CreateHotelServiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var createdService = await _hotelServiceService.CreateHotelServiceAsync(request);
            return CreatedAtAction(nameof(GetServiceById), new { id = createdService.Id }, createdService);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F08-02 &amp; UC-F08-06 - Xem danh sách, tìm kiếm và lọc dịch vụ khách sạn.
    /// Hỗ trợ tìm theo từ khóa, lọc theo trạng thái hoạt động, khoảng giá từ-đến, phân trang và sắp xếp.
    /// 
    /// API:
    /// GET https://localhost:7024/HotelOps/services
    /// </summary>
    /// <param name="parameters">Các tiêu chí tìm kiếm, lọc và phân trang.</param>
    /// <returns>Danh sách dịch vụ phân trang thỏa mãn điều kiện.</returns>
    /// <response code="200">Trả về danh sách dịch vụ thành công.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HotelServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices([FromQuery] HotelServiceQueryParameters parameters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _hotelServiceService.GetHotelServicesAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// UC-F08-03 - Xem chi tiết dịch vụ.
    /// Truy vấn theo ID, trả về thông tin chi tiết kèm thống kê sử dụng và doanh thu.
    /// 
    /// API:
    /// GET https://localhost:7024/HotelOps/services/{id}
    /// </summary>
    /// <param name="id">Mã định danh dịch vụ.</param>
    /// <returns>Thông tin chi tiết dịch vụ.</returns>
    /// <response code="200">Tìm thấy dịch vụ.</response>
    /// <response code="404">Không tìm thấy dịch vụ với mã tương ứng.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HotelServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById([FromRoute] int id)
    {
        var service = await _hotelServiceService.GetHotelServiceByIdAsync(id);
        if (service == null)
        {
            return NotFound(new { message = $"Không tìm thấy dịch vụ với ID = {id}." });
        }

        return Ok(service);
    }

    /// <summary>
    /// UC-F08-04 - Cập nhật dịch vụ.
    /// Chỉnh sửa thông tin, đơn vị tính, mô tả, giá và trạng thái.
    /// Việc đổi giá chỉ áp dụng cho tương lai, bảo toàn đơn giá các lượt sử dụng trong quá khứ.
    /// 
    /// API:
    /// PUT https://localhost:7024/HotelOps/services/{id}
    /// </summary>
    /// <param name="id">Mã định danh dịch vụ cần sửa.</param>
    /// <param name="request">Dữ liệu cập nhật mới.</param>
    /// <returns>Thông tin dịch vụ sau khi cập nhật.</returns>
    /// <response code="200">Cập nhật dịch vụ thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc mã dịch vụ bị trùng với dịch vụ khác.</response>
    /// <response code="404">Không tìm thấy dịch vụ cần sửa.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HotelServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateService([FromRoute] int id, [FromBody] UpdateHotelServiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updatedService = await _hotelServiceService.UpdateHotelServiceAsync(id, request);
            if (updatedService == null)
            {
                return NotFound(new { message = $"Không tìm thấy dịch vụ với ID = {id}." });
            }

            return Ok(updatedService);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// UC-F08-05 - Xóa hoặc ngừng cung cấp dịch vụ.
    /// Áp dụng quy tắc bảo toàn dữ liệu tính phí: Xóa vật lý nếu chưa phát sinh lượt dùng nào;
    /// Tự động chuyển IsActive = false nếu đã có lịch sử sử dụng.
    /// 
    /// API:
    /// DELETE https://localhost:7024/HotelOps/services/{id}
    /// </summary>
    /// <param name="id">Mã định danh dịch vụ cần xóa hoặc ngừng hoạt động.</param>
    /// <returns>Kết quả thực hiện thao tác xóa hoặc ngừng cung cấp.</returns>
    /// <response code="200">Thao tác thành công.</response>
    /// <response code="404">Không tìm thấy dịch vụ cần thao tác.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteHotelServiceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrDeactivateService([FromRoute] int id)
    {
        var result = await _hotelServiceService.DeleteOrDeactivateHotelServiceAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy dịch vụ với ID = {id}." });
        }

        return Ok(result);
    }
}
