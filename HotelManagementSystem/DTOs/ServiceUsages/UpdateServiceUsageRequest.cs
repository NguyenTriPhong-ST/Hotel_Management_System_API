
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.ServiceUsages;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-04 - Update Service Usage
// Mô tả: Nhận số lượng và ghi chú cần cập nhật.
// OData: Không dùng cho PUT.
// =====================================================

public class UpdateServiceUsageRequest
{
    [Range(
        typeof(decimal),
        "0.01",
        "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Quantity phải lớn hơn 0 và không vượt quá giới hạn."
    )]
    public decimal Quantity { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
