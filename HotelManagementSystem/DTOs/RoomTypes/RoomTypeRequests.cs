using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.DTOs.RoomTypes;

public sealed class CreateRoomTypeRequest
{
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "9999999999999999.99",
    ParseLimitsInInvariantCulture = true)]
    public decimal BasePricePerNight { get; set; }
}

public sealed class UpdateRoomTypeRequest
{
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "9999999999999999.99",
    ParseLimitsInInvariantCulture = true)]
    public decimal BasePricePerNight { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class RoomTypeListQuery
{
    public string? Name { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinCapacity { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "9999999999999999.99",
    ParseLimitsInInvariantCulture = true)]
    public decimal? MinPrice { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "9999999999999999.99",
    ParseLimitsInInvariantCulture = true)]
    public decimal? MaxPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "name";
    public string SortDirection { get; set; } = "asc";
}
