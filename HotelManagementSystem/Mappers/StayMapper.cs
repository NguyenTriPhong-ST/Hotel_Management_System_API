
using System.Linq.Expressions;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;


// =====================================================
// Developer: NhaNT
// Module: F07 - Stay Management
// Layer: Mapper
//
// UC-F07-02: Stay -> StayListDto.
// UC-F07-03: Stay -> StayDetailDto.
// UC-F07-03: StayGuest -> StayGuestDetailDto.
// UC-F07-03: ServiceUsage -> StayServiceUsageDetailDto.
//
// Sử dụng Expression để EF Core dịch thành SQL.
// =====================================================

public static class StayMapper
{
    // =================================================
    // NhaNT | F07-02, F07-07
    // Expression để EF Core dịch truy vấn thành SQL.
    // =================================================
    public static readonly Expression<Func<Stay, StayListDto>>
        ToListDto = s => new StayListDto
        {
            Id = s.Id,
            StayCode = s.StayCode,

            ReservationId = s.ReservationId,
            ReservationCode =
                s.Reservation.ReservationCode,

            RoomId = s.RoomId,
            RoomCode = s.Room.RoomCode,

            MainGuestId = s.Reservation.GuestId,
            MainGuestName =
                s.Reservation.Guest.FullName,

            CheckedInAt = s.CheckedInAt,
            CheckedOutAt = s.CheckedOutAt,

            Status = s.CheckedOutAt == null
                ? "Active"
                : "Completed"
        };

    // =================================================
    // NhaNT | F07-03
    // Chuyển Stay thành DTO chi tiết.
    // =================================================
    public static readonly Expression<Func<Stay, StayDetailDto>>
        ToDetailDto = s => new StayDetailDto
        {
            Id = s.Id,
            StayCode = s.StayCode,

            ReservationId = s.ReservationId,
            ReservationCode =
                s.Reservation.ReservationCode,

            CheckInDate = s.Reservation.CheckInDate,
            CheckOutDate = s.Reservation.CheckOutDate,
            NumberOfGuests = s.Reservation.NumberOfGuests,
            NightlyRate = s.Reservation.NightlyRate,

            RoomId = s.RoomId,
            RoomCode = s.Room.RoomCode,

            CheckedInAt = s.CheckedInAt,
            CheckedOutAt = s.CheckedOutAt,

            CheckedInByAccountId =
                s.CheckedInByAccountId,
            CheckedOutByAccountId =
                s.CheckedOutByAccountId,

            Notes = s.Notes,

            Status = s.CheckedOutAt == null
                ? "Active"
                : "Completed"
        };

    // =================================================
    // NhaNT | F07-03
    // Chuyển StayGuest sang DTO thông tin khách.
    // =================================================
    public static readonly
        Expression<Func<StayGuest, StayGuestDetailDto>>
        ToGuestDto = sg => new StayGuestDetailDto
        {
            GuestId = sg.GuestId,
            FullName = sg.Guest.FullName,
            IsMainGuest = sg.IsMainGuest
        };

    // =================================================
    // NhaNT | F07-03
    // Sử dụng giá snapshot của ServiceUsage.
    // =================================================
    public static readonly
        Expression<Func<ServiceUsage, StayServiceUsageDetailDto>>
        ToServiceUsageDto = su =>
            new StayServiceUsageDetailDto
            {
                Id = su.Id,
                HotelServiceId = su.HotelServiceId,
                ServiceName = su.ServiceNameSnapshot,
                Unit = su.UnitSnapshot,
                Quantity = su.Quantity,
                UnitPrice = su.UnitPrice,
                Amount = su.Quantity * su.UnitPrice,
                UsedAt = su.UsedAt
            };
}
