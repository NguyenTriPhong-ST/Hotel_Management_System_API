using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.DTOs.RoomTypes;
using HotelManagementSystem.Services;

namespace HotelManagementSystem.Controllers;

[ApiController]
[Route("HotelOps/room-types")]
[Produces("application/json")]
// TODO (FE-01): bật [Authorize(Roles = "Admin")] sau khi cấu hình JWT trong Program.cs.
public sealed class RoomTypesController : ControllerBase
{
    private readonly RoomTypeService _service;

    public RoomTypesController(RoomTypeService service) => _service = service;

    /// <summary>
    /// UC-F04-01 - Thêm loại phòng.
    /// POST https://localhost:7024/HotelOps/room-types
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRoomTypeRequest request)
        => ExecuteAsync(async () =>
        {
            var created = await _service.CreateAsync(request, ActorId(), ActorName(), HttpContext.TraceIdentifier);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        });

    /// <summary>
    /// UC-F04-02, UC-F04-06 - Xem, tìm kiếm và lọc danh sách loại phòng.
    /// GET https://localhost:7024/HotelOps/room-types
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetList([FromQuery] RoomTypeListQuery query)
        => ExecuteAsync(async () => Ok(await _service.GetListAsync(query)));

    /// <summary>
    /// UC-F04-03 - Xem chi tiết loại phòng.
    /// GET https://localhost:7024/HotelOps/room-types/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id)
        => ExecuteAsync(async () => Ok(await _service.GetByIdAsync(id)));

    /// <summary>
    /// UC-F04-04 - Cập nhật loại phòng.
    /// PUT https://localhost:7024/HotelOps/room-types/{id}
    /// </summary>

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRoomTypeRequest request)
        => ExecuteAsync(async () => Ok(
            await _service.UpdateAsync(
                id,
                request,
                ActorId(),
                ActorName(),
                HttpContext.TraceIdentifier
            )
        ));


    /// <summary>
    /// UC-F04-05 - Lưu trữ mềm loại phòng.
    /// DELETE https://localhost:7024/HotelOps/room-types/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(async () =>
        {
            await _service.DeleteAsync(id, ActorId(), ActorName(), HttpContext.TraceIdentifier);
            return NoContent();
        });

    private int? ActorId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string? ActorName() => User.Identity?.Name;

    // Chuyển lỗi nghiệp vụ về HTTP Status Code rõ ràng để Swagger dễ kiểm thử.
    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (RoomTypeException ex)
        {
            var status = ex.Kind switch
            {
                RoomTypeErrorKind.Invalid => StatusCodes.Status400BadRequest,
                RoomTypeErrorKind.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            return Problem(statusCode: status, title: "Room Type operation failed", detail: ex.Message);
        }
    }
}
