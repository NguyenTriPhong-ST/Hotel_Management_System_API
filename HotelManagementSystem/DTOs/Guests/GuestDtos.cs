using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Guests;

/// <summary>
/// DTO trả về thông tin tóm tắt của khách hàng.
/// </summary>
public class GuestDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentTypeName => DocumentType switch
    {
        HotelManagementSystem.Entities.Enums.DocumentType.NationalId => "CCCD",
        HotelManagementSystem.Entities.Enums.DocumentType.Passport => "Hộ chiếu",
        _ => null
    };
    public string? DocumentNumber { get; set; }
    public string? DocumentCountryCode { get; set; }
    public string? Address { get; set; }
    public int TotalReservationsCount { get; set; }
    public int TotalStaysCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO chi tiết hồ sơ khách hàng kèm lịch sử đặt phòng và lưu trú (UC-F05-03).
/// </summary>
public class GuestDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentTypeName => DocumentType switch
    {
        HotelManagementSystem.Entities.Enums.DocumentType.NationalId => "CCCD",
        HotelManagementSystem.Entities.Enums.DocumentType.Passport => "Hộ chiếu",
        _ => null
    };
    public string? DocumentNumber { get; set; }
    public string? DocumentCountryCode { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<GuestReservationSummaryDto> Reservations { get; set; } = new();
    public List<GuestStaySummaryDto> Stays { get; set; } = new();
}

public class GuestReservationSummaryDto
{
    public int Id { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal NightlyRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GuestStaySummaryDto
{
    public int Id { get; set; }
    public string StayCode { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public bool IsMainGuest { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
}
