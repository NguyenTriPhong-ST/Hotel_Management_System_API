
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// Module: F09 - Service Usage Management
// Swagger: https://localhost:7024/swagger
//
// UC-F09-01: POST   /HotelOps/service-usages
// UC-F09-02: GET    /HotelOps/service-usages
// UC-F09-03: GET    /HotelOps/service-usages/{id}
// UC-F09-04: PUT    /HotelOps/service-usages/{id}
// UC-F09-05: DELETE /HotelOps/service-usages/{id}
//
// Header ghi dữ liệu: X-Demo-Account-Id
// =====================================================

[ApiController]
[Route("HotelOps/service-usages")]
public class ServiceUsagesController : ControllerBase
{
    private readonly ServiceUsageService _service;
    private readonly IWebHostEnvironment _environment;

    public ServiceUsagesController(
        ServiceUsageService service,
        IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-01: Create Service Usage
    // POST /HotelOps/service-usages
    // Body: CreateServiceUsageRequest
    // Header: X-Demo-Account-Id
    // =================================================
    [HttpPost]
    [ProducesResponseType(typeof(CreateServiceUsageResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceUsageRequest request,
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
            var result = await _service.CreateAsync(
                request,
                demoAccountId.Value);

            return StatusCode(201, result);
        }
        catch (ServiceUsageBusinessException ex)
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
                message = "Xung đột dữ liệu khi ghi nhận dịch vụ."
            });
        }
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-02: View Service Usages
    // GET /HotelOps/service-usages
    //
    // Query: search, stayId, hotelServiceId,
    //        page, pageSize, sort
    //
    // GET /HotelOps/service-usages?page=1&pageSize=20
    // GET /HotelOps/service-usages?stayId=20
    // GET /HotelOps/service-usages?hotelServiceId=1
    // GET /HotelOps/service-usages?search=P101
    // GET /HotelOps/service-usages?sort=oldest
    // =================================================
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetServiceUsages(
        [FromQuery] string? search,
        [FromQuery] int? stayId,
        [FromQuery] int? hotelServiceId,
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

        if (stayId.HasValue && stayId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "stayId phải lớn hơn 0."
            });
        }

        if (hotelServiceId.HasValue &&
            hotelServiceId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "hotelServiceId phải lớn hơn 0."
            });
        }

        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "page vượt giới hạn phân trang."
            });
        }

        var normalizedSort = (sort ?? "newest")
            .Trim()
            .ToLowerInvariant();

        if (normalizedSort != "newest" &&
            normalizedSort != "oldest")
        {
            return BadRequest(new
            {
                message = "sort chỉ nhận newest hoặc oldest."
            });
        }

        var query = _service.GetServiceUsages();

        // NhaNT | F09-02
        // Lọc lượt sử dụng theo Stay.
        if (stayId.HasValue)
        {
            query = query.Where(x =>
                x.StayId == stayId.Value);
        }

        // NhaNT | F09-02
        // Lọc theo dịch vụ khách sạn.
        if (hotelServiceId.HasValue)
        {
            query = query.Where(x =>
                x.HotelServiceId == hotelServiceId.Value);
        }

        // NhaNT | F09-02
        // Tìm theo mã Stay, phòng và tên dịch vụ.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(x =>
                x.StayCode.Contains(keyword) ||
                x.RoomCode.Contains(keyword) ||
                x.ServiceName.Contains(keyword));
        }

        // NhaNT | F09-02
        // Sắp xếp và phân trang tại Database.
        query = normalizedSort == "oldest"
            ? query.OrderBy(x => x.Id)
            : query.OrderByDescending(x => x.Id);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((int)offset)
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
    // UC-F09-03: View Service Usage Details
    // GET /HotelOps/service-usages/{id}
    // =================================================
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceUsageDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDetail(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "ServiceUsage ID phải lớn hơn 0."
            });
        }

        var result = await _service.GetDetailAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy lượt sử dụng dịch vụ."
            });
        }

        return Ok(result);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-04: Update Service Usage
    // PUT /HotelOps/service-usages/{id}
    // Body: UpdateServiceUsageRequest
    // Header: X-Demo-Account-Id
    // =================================================
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CreateServiceUsageResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateServiceUsageRequest request,
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

        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "ServiceUsage ID phải lớn hơn 0."
            });
        }

        try
        {
            var result = await _service.UpdateAsync(
                id,
                request,
                demoAccountId.Value);

            return Ok(result);
        }
        catch (ServiceUsageBusinessException ex)
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
                message = "Xung đột dữ liệu khi cập nhật dịch vụ."
            });
        }
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-05: Delete Service Usage
    // DELETE /HotelOps/service-usages/{id}
    // Header: X-Demo-Account-Id
    // Xóa mềm, không xóa vật lý dữ liệu.
    // =================================================
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Delete(
        int id,
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

        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "ServiceUsage ID phải lớn hơn 0."
            });
        }

        try
        {
            await _service.DeleteAsync(
                id,
                demoAccountId.Value);

            return NoContent();
        }
        catch (ServiceUsageBusinessException ex)
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
                message = "Xung đột dữ liệu khi xóa dịch vụ."
            });
        }
    }
}
