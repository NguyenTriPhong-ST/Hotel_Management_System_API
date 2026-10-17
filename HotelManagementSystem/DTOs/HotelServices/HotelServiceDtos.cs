namespace HotelManagementSystem.DTOs.HotelServices;

/// <summary>
/// DTO trả về thông tin tóm tắt của dịch vụ khách sạn.
/// </summary>
public class HotelServiceDto
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; }
    public int TotalUsagesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO chi tiết dịch vụ khách sạn (UC-F08-03).
/// </summary>
public class HotelServiceDetailDto
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; }
    public int TotalUsagesCount { get; set; }
    public decimal TotalQuantityUsed { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool CanBePermanentlyDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Kết quả xóa hoặc ngừng cung cấp dịch vụ (UC-F08-05).
/// </summary>
public class DeleteHotelServiceResultDto
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPermanentlyDeleted { get; set; }
    public string Message { get; set; } = string.Empty;
}
