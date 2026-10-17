using System.ComponentModel.DataAnnotations;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Guests;

/// <summary>
/// UC-F05-01 - Tạo hồ sơ khách.
/// </summary>
public class CreateGuestRequest : IValidatableObject
{
    [Required(ErrorMessage = "Họ tên khách hàng là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    [RegularExpression(@"^[0-9+\s-]*$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? PhoneNumber { get; set; }

    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    public string? Email { get; set; }

    public DocumentType? DocumentType { get; set; }

    [StringLength(30, ErrorMessage = "Số giấy tờ không được vượt quá 30 ký tự.")]
    public string? DocumentNumber { get; set; }

    [StringLength(3, ErrorMessage = "Mã quốc gia không được vượt quá 3 ký tự.")]
    public string? DocumentCountryCode { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasType = DocumentType.HasValue;
        var hasNumber = !string.IsNullOrWhiteSpace(DocumentNumber);
        var hasCountry = !string.IsNullOrWhiteSpace(DocumentCountryCode);

        var count = (hasType ? 1 : 0) + (hasNumber ? 1 : 0) + (hasCountry ? 1 : 0);
        if (count > 0 && count < 3)
        {
            yield return new ValidationResult(
                "Ba trường giấy tờ định danh (Loại giấy tờ, Số giấy tờ, Mã quốc gia) phải cùng được cung cấp đầy đủ hoặc cùng để trống.",
                new[] { nameof(DocumentType), nameof(DocumentNumber), nameof(DocumentCountryCode) }
            );
        }
    }
}

/// <summary>
/// UC-F05-04 - Cập nhật hồ sơ khách.
/// </summary>
public class UpdateGuestRequest : IValidatableObject
{
    [Required(ErrorMessage = "Họ tên khách hàng là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    [RegularExpression(@"^[0-9+\s-]*$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? PhoneNumber { get; set; }

    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    public string? Email { get; set; }

    public DocumentType? DocumentType { get; set; }

    [StringLength(30, ErrorMessage = "Số giấy tờ không được vượt quá 30 ký tự.")]
    public string? DocumentNumber { get; set; }

    [StringLength(3, ErrorMessage = "Mã quốc gia không được vượt quá 3 ký tự.")]
    public string? DocumentCountryCode { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasType = DocumentType.HasValue;
        var hasNumber = !string.IsNullOrWhiteSpace(DocumentNumber);
        var hasCountry = !string.IsNullOrWhiteSpace(DocumentCountryCode);

        var count = (hasType ? 1 : 0) + (hasNumber ? 1 : 0) + (hasCountry ? 1 : 0);
        if (count > 0 && count < 3)
        {
            yield return new ValidationResult(
                "Ba trường giấy tờ định danh (Loại giấy tờ, Số giấy tờ, Mã quốc gia) phải cùng được cung cấp đầy đủ hoặc cùng để trống.",
                new[] { nameof(DocumentType), nameof(DocumentNumber), nameof(DocumentCountryCode) }
            );
        }
    }
}

/// <summary>
/// UC-F05-02 &amp; UC-F05-06 - Tham số tìm kiếm, lọc và phân trang danh sách khách hàng.
/// </summary>
public class GuestQueryParameters
{
    public string? Keyword { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DocumentNumber { get; set; }
    public bool? HasStayHistory { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
}
