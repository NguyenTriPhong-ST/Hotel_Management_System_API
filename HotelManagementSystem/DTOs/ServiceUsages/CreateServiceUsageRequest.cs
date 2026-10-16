
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.ServiceUsages;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-01 - Create Service Usage
// Mô tả: Kiểm tra dữ liệu ghi nhận dịch vụ.
// OData: Không sử dụng cho POST.
// =====================================================


/*
 * =====================================================
 * Developer: NhaNTCE181298
 * UC: UC-F09-01 - Create Service Usage
 * Endpoint: POST /api/service-usages
 * Swagger: https://localhost:7024/swagger
 * 
 * 
 * Request body
{
  "stayId": 16,
  "hotelServiceId": 1,
  "quantity": 2,
  "notes": "Test UC-F09-01 - Invoice da Finalized."
}

 *
 * TEST CASES:
 * 1. Valid: StayId=20, ServiceId=1, Qty=2
 *    -> 201 Created, Amount=180000.
 *
 * 2. Unauthorized Role: AccountId=1
 *    -> 403 Forbidden.
 *
 * 3. Stay Not Found: StayId=99999
 *    -> 404 Not Found.
 *
 * 4. Inactive Service: ServiceId=9
 *    -> 409 Conflict.
 *
 * 5. Checked-out Stay: StayId=1
 *    -> 409 Conflict.
 *
 * 6. Invalid Quantity: Quantity=0
 *    -> 400 Bad Request.
 *
 * 7. Finalized Invoice: StayId=16
 *    -> 409 Conflict.
 *
 * 8. SQL Verification:
 *    -> ServiceUsage ID 58, AuditLog ID 55.
 *
 * NOTE:
 * - X-Demo-Account-Id=2: Receptionist test.
 * - OData không áp dụng cho thao tác POST.
 * - Thay header demo bằng JWT trước khi triển khai.
 * =====================================================
 */


public class CreateServiceUsageRequest
{
    [Range(1, int.MaxValue,
        ErrorMessage = "StayId phải lớn hơn 0.")]
    public int StayId { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "HotelServiceId phải lớn hơn 0.")]
    public int HotelServiceId { get; set; }

    // NhaNTCE181298 | UC-F09-01
    // Parse giới hạn decimal bằng InvariantCulture,
    // tránh lỗi khi máy sử dụng dấu phẩy thập phân.
    [Range(
        typeof(decimal),
        "0.01",
        "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Quantity phải lớn hơn 0 và không vượt quá giới hạn."
    )]
    public decimal Quantity { get; set; }

    [MaxLength(1000,
        ErrorMessage = "Ghi chú không vượt quá 1000 ký tự.")]
    public string? Notes { get; set; }
}
