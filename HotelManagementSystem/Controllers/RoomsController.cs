using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.DTOs.Rooms;
using HotelManagementSystem.Services;

namespace HotelManagementSystem.Controllers;

[ApiController]
[Route("HotelOps/rooms")]
[Produces("application/json")]
// TODO (FE-01): Sau khi JWT cấu hình xong, bật [Authorize(Roles = "Admin,Receptionist")].
public sealed class RoomsController : ControllerBase
{
    private readonly RoomService _service;

    public RoomsController(RoomService service) => _service = service;

    /// <summary>
    /// UC-F03-01 - Thêm phòng.
    /// POST https://localhost:7024/HotelOps/rooms
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateRoomRequest request)
        => ExecuteAsync(async () =>
        {
            var created = await _service.CreateAsync(request, ActorId(), ActorName(), HttpContext.TraceIdentifier);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        });

    /// <summary>
    /// UC-F03-02, UC-F03-07 - Danh sách, tìm kiếm và lọc phòng.
    /// GET https://localhost:7024/HotelOps/rooms
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(RoomPagedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetList([FromQuery] RoomListQuery query)
        => ExecuteAsync(async () => Ok(await _service.GetListAsync(query)));

    /// <summary>
    /// UC-F03-03 - Xem chi tiết phòng.
    /// GET https://localhost:7024/HotelOps/rooms/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id)
        => ExecuteAsync(async () => Ok(await _service.GetByIdAsync(id)));

    /// <summary>
    /// UC-F03-04 - Cập nhật thông tin phòng.
    /// PUT https://localhost:7024/HotelOps/rooms/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRoomRequest request)
        => ExecuteAsync(async () => Ok(await _service.UpdateAsync(
            id, request, ActorId(), ActorName(), HttpContext.TraceIdentifier)));

    /// <summary>
    /// UC-F03-06 - Cập nhật trạng thái phòng.
    /// PATCH https://localhost:7024/HotelOps/rooms/{id}/status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRoomStatusRequest request)
        => ExecuteAsync(async () => Ok(await _service.UpdateStatusAsync(
            id, request, ActorId(), ActorName(), HttpContext.TraceIdentifier)));

    /// <summary>
    /// UC-F03-05 - Lưu trữ mềm phòng.
    /// DELETE https://localhost:7024/HotelOps/rooms/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(async () =>
        {
            await _service.DeleteAsync(id, ActorId(), ActorName(), HttpContext.TraceIdentifier);
            return NoContent();
        });

    private int? ActorId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string? ActorName() => User.Identity?.Name;

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (RoomException ex)
        {
            var status = ex.Kind switch
            {
                RoomErrorKind.Invalid => StatusCodes.Status400BadRequest,
                RoomErrorKind.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            return Problem(statusCode: status, title: "Room operation failed", detail: ex.Message);
        }
    }
}
