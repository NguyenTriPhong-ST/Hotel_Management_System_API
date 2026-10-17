using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.HotelServices;

/// <summary>
/// UC-F08-01 - Thêm dịch vụ khách sạn.
/// </summary>
public class CreateHotelServiceRequest
{
    [Required(ErrorMessage = "Mã dịch vụ là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Mã dịch vụ không được vượt quá 30 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "Mã dịch vụ chỉ được chứa chữ cái, số, gạch ngang và gạch dưới.")]
    public string ServiceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tên dịch vụ không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Đơn vị tính là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Đơn vị tính không được vượt quá 30 ký tự.")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đơn giá dịch vụ là bắt buộc.")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Đơn giá dịch vụ không được âm.")]
    public decimal CurrentPrice { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// UC-F08-04 - Cập nhật dịch vụ khách sạn.
/// </summary>
public class UpdateHotelServiceRequest
{
    [Required(ErrorMessage = "Mã dịch vụ là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Mã dịch vụ không được vượt quá 30 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "Mã dịch vụ chỉ được chứa chữ cái, số, gạch ngang và gạch dưới.")]
    public string ServiceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tên dịch vụ không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Đơn vị tính là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Đơn vị tính không được vượt quá 30 ký tự.")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đơn giá dịch vụ là bắt buộc.")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Đơn giá dịch vụ không được âm.")]
    public decimal CurrentPrice { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// UC-F08-02 &amp; UC-F08-06 - Tham số tìm kiếm, lọc và phân trang danh mục dịch vụ.
/// </summary>
public class HotelServiceQueryParameters : IValidatableObject
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Giá tối thiểu không được âm.")]
    public decimal? MinPrice { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Giá tối đa không được âm.")]
    public decimal? MaxPrice { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "Name";
    public string? SortOrder { get; set; } = "asc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value)
        {
            yield return new ValidationResult(
                "Giá tối thiểu (MinPrice) không được lớn hơn giá tối đa (MaxPrice).",
                new[] { nameof(MinPrice), nameof(MaxPrice) }
            );
        }
    }
}
