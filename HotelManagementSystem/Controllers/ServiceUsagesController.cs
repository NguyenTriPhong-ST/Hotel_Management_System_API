
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;


namespace HotelManagementSystem.Controllers;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-01 - Create Service Usage
// Endpoint: POST /api/service-usages
// OData: Không áp dụng cho POST.
//
// DEMO:
// Swagger: https://localhost:7024/swagger
// Header: X-Demo-Account-Id = 2
//
// Account ID 2: Receptionist mẫu.
// Account ID 1: Admin, không được phép ghi nhận.
//
// NOTE: Header demo chỉ dùng trong Development.
// =====================================================




[ApiController]
[Route("api/service-usages")]
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

    // NhaNTCE181298 | UC-F09-01
    // Ghi nhận một lượt sử dụng dịch vụ.
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceUsageRequest request,
        [FromHeader(Name = "X-Demo-Account-Id")]
        int? demoAccountId)
    {
        // Không cho dùng cơ chế header demo ở Production.
        if (!_environment.IsDevelopment())
        {
            return StatusCode(503, new
            {
                message = "API đang chờ tích hợp JWT."
            });
        }

        if (!demoAccountId.HasValue || demoAccountId <= 0)
        {
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });
        }

        try
        {
            var result = await _service.CreateAsync(
                request, demoAccountId.Value);

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
                message = "Không thể ghi nhận dịch vụ do xung đột dữ liệu."
            });
        }
    }

    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F09-02 - View Service Usages
    // Endpoint: GET /api/service-usages
    // OData: $filter, $orderby, $top, $skip, $select
    // =====================================================

    [HttpGet]
    [EnableQuery(PageSize = 20, MaxTop = 100)]
    public IQueryable<ServiceUsageListDto> GetServiceUsages()
    {
        // API chỉ đọc, không thay đổi Database.
        return _service.GetServiceUsages();
    }


    /*
     * =====================================================
     * Developer: NhaNTCE181298
     * UC: UC-F09-03 - View Service Usage Details
     * Endpoint: GET /api/service-usages/{id}
     *
     * DEMO LINKS:
     *
     * https://localhost:7024/api/service-usages/58
     * -> Dịch vụ ăn sáng của Stay 20, 180.000 VND.
     *
     * https://localhost:7024/api/service-usages/1
     * -> Dịch vụ lịch sử của Stay 1, 85.000 VND.
     *
     * https://localhost:7024/api/service-usages/99999
     * -> Kiểm tra HTTP 404 Not Found.
     *
     * NOTE:
     * - Chỉ hiển thị ServiceUsage chưa soft-delete.
     * - Giá sử dụng là snapshot, không phải giá hiện tại.
     * - OData không áp dụng cho GET chi tiết theo ID.
     * =====================================================
     */

    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F09-03 - View Service Usage Details
    // Endpoint: GET /api/service-usages/{id}
    //
    // DEMO LINKS:
    //
    // https://localhost:7024/api/service-usages/58
    // -> Dịch vụ ăn sáng vừa tạo cho Stay 20.
    //
    // https://localhost:7024/api/service-usages/1
    // -> Dịch vụ mẫu của Stay đã hoàn thành.
    //
    // https://localhost:7024/api/service-usages/99999
    // -> Kiểm tra HTTP 404.
    //
    // OData: Không áp dụng cho API chi tiết.
    // =====================================================

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ServiceUsageDetailDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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


    // =====================================================
    // Developer: NhaNTCE181298
    // UC-F09-04: Update Service Usage
    // Endpoint: PUT /api/service-usages/{id}
    // OData: Không sử dụng cho PUT.
    // =====================================================

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
            return StatusCode(503, new
            {
                message = "API đang chờ tích hợp JWT."
            });

        if (!demoAccountId.HasValue || demoAccountId <= 0)
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });

        if (id <= 0)
            return BadRequest(new
            {
                message = "ServiceUsage ID phải lớn hơn 0."
            });

        try
        {
            var result = await _service.UpdateAsync(
                id, request, demoAccountId.Value);

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

    // =====================================================
    // Developer: NhaNTCE181298
    // UC-F09-05: Delete Service Usage
    // Endpoint: DELETE /api/service-usages/{id}
    // OData: Không sử dụng cho DELETE.
    // =====================================================

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
            return StatusCode(503, new
            {
                message = "API đang chờ tích hợp JWT."
            });

        if (!demoAccountId.HasValue || demoAccountId <= 0)
            return BadRequest(new
            {
                message = "Thiếu X-Demo-Account-Id hợp lệ."
            });

        if (id <= 0)
            return BadRequest(new
            {
                message = "ServiceUsage ID phải lớn hơn 0."
            });

        try
        {
            await _service.DeleteAsync(
                id, demoAccountId.Value);

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
