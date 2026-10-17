using HotelManagementSystem.DTOs.Guests;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;

/// <summary>
/// Chuyển đổi dữ liệu (Mapping) giữa Entity Guest và các DTO tương ứng.
/// </summary>
public static class GuestMapper
{
    public static Guest ToEntity(CreateGuestRequest request)
    {
        return new Guest
        {
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            DocumentType = request.DocumentType,
            DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? null : request.DocumentNumber.Trim(),
            DocumentCountryCode = string.IsNullOrWhiteSpace(request.DocumentCountryCode) ? null : request.DocumentCountryCode.Trim().ToUpper(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
    }

    public static void UpdateEntity(Guest guest, UpdateGuestRequest request)
    {
        guest.FullName = request.FullName.Trim();
        guest.DateOfBirth = request.DateOfBirth;
        guest.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        guest.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        guest.DocumentType = request.DocumentType;
        guest.DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? null : request.DocumentNumber.Trim();
        guest.DocumentCountryCode = string.IsNullOrWhiteSpace(request.DocumentCountryCode) ? null : request.DocumentCountryCode.Trim().ToUpper();
        guest.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        guest.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
    }

    public static GuestDto ToDto(Guest guest)
    {
        return new GuestDto
        {
            Id = guest.Id,
            FullName = guest.FullName,
            DateOfBirth = guest.DateOfBirth,
            PhoneNumber = guest.PhoneNumber,
            Email = guest.Email,
            DocumentType = guest.DocumentType,
            DocumentNumber = guest.DocumentNumber,
            DocumentCountryCode = guest.DocumentCountryCode,
            Address = guest.Address,
            TotalReservationsCount = guest.Reservations?.Count ?? 0,
            TotalStaysCount = guest.StayGuests?.Count ?? 0,
            CreatedAt = guest.CreatedAt,
            UpdatedAt = guest.UpdatedAt
        };
    }

    public static GuestDetailDto ToDetailDto(Guest guest)
    {
        return new GuestDetailDto
        {
            Id = guest.Id,
            FullName = guest.FullName,
            DateOfBirth = guest.DateOfBirth,
            PhoneNumber = guest.PhoneNumber,
            Email = guest.Email,
            DocumentType = guest.DocumentType,
            DocumentNumber = guest.DocumentNumber,
            DocumentCountryCode = guest.DocumentCountryCode,
            Address = guest.Address,
            Notes = guest.Notes,
            CreatedAt = guest.CreatedAt,
            UpdatedAt = guest.UpdatedAt,
            Reservations = guest.Reservations?.Select(r => new GuestReservationSummaryDto
            {
                Id = r.Id,
                ReservationCode = r.ReservationCode,
                RoomCode = r.Room?.RoomCode ?? string.Empty,
                RoomTypeName = r.RoomType?.Name ?? string.Empty,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                NumberOfGuests = r.NumberOfGuests,
                NightlyRate = r.NightlyRate,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            }).OrderByDescending(r => r.CreatedAt).ToList() ?? new(),
            Stays = guest.StayGuests?.Select(sg => new GuestStaySummaryDto
            {
                Id = sg.StayId,
                StayCode = sg.Stay?.StayCode ?? string.Empty,
                RoomCode = sg.Stay?.Room?.RoomCode ?? string.Empty,
                IsMainGuest = sg.IsMainGuest,
                CheckedInAt = sg.Stay?.CheckedInAt ?? sg.AddedAt,
                CheckedOutAt = sg.Stay?.CheckedOutAt
            }).OrderByDescending(s => s.CheckedInAt).ToList() ?? new()
        };
    }
}
