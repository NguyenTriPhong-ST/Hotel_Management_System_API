
using System.Text.Json;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Repositories.Interfaces;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNT
// Module: F07 - Stay Management
// Layer: Service
//
// UC-F07-01: Check-in
// UC-F07-02: View Stays
// UC-F07-03: View Stay Detail
// UC-F07-06: Check-out
// UC-F07-07: Search & Filter Stays
//
// Swagger: https://localhost:7024/swagger
// POST /api/stays/check-in
// GET  /api/stays/{id}
// POST /api/stays/{id}/check-out
//
// OData:
// http://localhost:5142/odata/StayViews?$top=10
//
// Xử lý nghiệp vụ qua IStayRepository.
// =====================================================

public class StayService
{
    private readonly IStayRepository _repository;

    public StayService(IStayRepository repository)
    {
        _repository = repository;
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-01: Check-in
    // API: POST /api/stays/check-in
    //
    // Kiểm tra Reservation, phòng, khách và tạo Stay.
    // Tất cả thay đổi được lưu trong một transaction.
    // =================================================

    public async Task<CheckInResponse> CheckInAsync(
        CheckInRequest request,
        int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F07-01
        // Kiểm tra tài khoản Receptionist.
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null ||
            !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new StayBusinessException(
                403,
                "Tài khoản không có quyền check-in.");
        }

        // NhaNT | F07-01
        // Reservation phải tồn tại và Confirmed.
        var reservation =
            await _repository.GetReservationForCheckInAsync(
                request.ReservationId);

        if (reservation == null)
        {
            throw new StayBusinessException(
                404,
                "Không tìm thấy Reservation.");
        }

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new StayBusinessException(
                409,
                "Reservation không ở trạng thái Confirmed.");
        }

        // NhaNT | F07-01
        // Kiểm tra ngày nhận phòng theo giờ Việt Nam.
        var now = DateTime.UtcNow;

        var vietnamToday = DateOnly.FromDateTime(
            now.AddHours(7));

        if (vietnamToday < reservation.CheckInDate ||
            vietnamToday >= reservation.CheckOutDate)
        {
            throw new StayBusinessException(
                409,
                "Ngày hiện tại nằm ngoài thời gian nhận phòng.");
        }

        // NhaNT | F07-01
        // Phòng phải hoạt động và đã Inspected.
        var room = reservation.Room;

        if (room.DeletedAt != null ||
            room.OperationalStatus !=
                RoomOperationalStatus.Active)
        {
            throw new StayBusinessException(
                409,
                "Phòng không còn hoạt động.");
        }

        if (room.HousekeepingStatus !=
            HousekeepingStatus.Inspected)
        {
            throw new StayBusinessException(
                409,
                "Phòng chưa đạt trạng thái Inspected.");
        }

        // NhaNT | F07-01
        // Không cho nhận phòng nếu đang có khách.
        var hasActiveStay =
            await _repository.HasActiveStayAsync(room.Id);

        if (hasActiveStay)
        {
            throw new StayBusinessException(
                409,
                "Phòng đang có khách lưu trú.");
        }

        // NhaNT | F07-01
        // Không cho nhận phòng đang bị khóa/bảo trì.
        var isBlocked =
            await _repository.IsRoomBlockedAsync(
                room.Id, now);

        if (isBlocked)
        {
            throw new StayBusinessException(
                409,
                "Phòng đang bị khóa hoặc bảo trì.");
        }

        // NhaNT | F07-01
        // Kiểm tra danh sách khách thực tế.
        var guestIds = request.GuestIds
            .Distinct()
            .ToList();

        if (guestIds.Count != request.GuestIds.Count)
        {
            throw new StayBusinessException(
                400,
                "Danh sách khách có ID trùng nhau.");
        }

        if (guestIds.Count == 0)
        {
            throw new StayBusinessException(
                400,
                "Phải có ít nhất một khách nhận phòng.");
        }

        // Khách đại diện phải tham gia nhận phòng.
        if (!guestIds.Contains(reservation.GuestId))
        {
            throw new StayBusinessException(
                400,
                "Thiếu khách đại diện của Reservation.");
        }

        // Không vượt sức chứa hoặc số lượng đã đặt.
        if (guestIds.Count > reservation.CapacitySnapshot)
        {
            throw new StayBusinessException(
                400,
                "Số khách vượt quá sức chứa đã chốt.");
        }

        if (guestIds.Count > reservation.NumberOfGuests)
        {
            throw new StayBusinessException(
                400,
                "Số khách vượt số lượng đã đặt.");
        }

        // NhaNT | F07-01
        // Kiểm tra toàn bộ Guest ID còn hợp lệ.
        var validGuestCount =
            await _repository.CountValidGuestsAsync(
                guestIds);

        if (validGuestCount != guestIds.Count)
        {
            throw new StayBusinessException(
                400,
                "Có khách không tồn tại hoặc đã bị lưu trữ.");
        }

        // NhaNT | F07-01
        // Chống check-in lặp cùng Reservation.
        var alreadyCheckedIn =
            await _repository.HasStayForReservationAsync(
                reservation.Id);

        if (alreadyCheckedIn)
        {
            throw new StayBusinessException(
                409,
                "Reservation đã được check-in trước đó.");
        }

        // NhaNT | F07-01
        // Tạo Stay với mã nghiệp vụ duy nhất.
        var stay = new Stay
        {
            StayCode = $"ST-{Guid.NewGuid():N}"[..19],
            ReservationId = reservation.Id,
            RoomId = room.Id,
            CheckedInAt = now,
            CheckedInByAccountId = accountId,
            Notes = request.Notes
        };

        // NhaNT | F07-01
        // Thêm khách thực tế và xác định khách chính.
        foreach (var guestId in guestIds)
        {
            stay.StayGuests.Add(new StayGuest
            {
                GuestId = guestId,
                IsMainGuest =
                    guestId == reservation.GuestId,
                AddedAt = now
            });
        }

        _repository.AddStay(stay);

        // NhaNT | F07-01
        // Chuyển Reservation sang CheckedIn.
        reservation.Status = ReservationStatus.CheckedIn;

        // Lưu để nhận StayId.
        await _repository.SaveChangesAsync();

        // NhaNT | F07-01
        // Ghi AuditLog trong cùng transaction.
        // Khi tích hợp LOG-01 cần tránh ghi log trùng.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "CheckIn",
            EntityType = "Stay",
            EntityId = stay.Id.ToString(),
            EntityCodeSnapshot = stay.StayCode,
            OccurredAt = now,
            Summary =
                $"Check-in Reservation " +
                $"{reservation.ReservationCode} " +
                $"vào phòng {room.RoomCode}.",
            CorrelationId = Guid.NewGuid().ToString()
        });

        await _repository.SaveChangesAsync();

        // NhaNT | F07-01
        // Commit khi tất cả thao tác thành công.
        await transaction.CommitAsync();

        return new CheckInResponse
        {
            StayId = stay.Id,
            StayCode = stay.StayCode,

            ReservationId = reservation.Id,
            ReservationCode = reservation.ReservationCode,

            RoomId = room.Id,
            RoomCode = room.RoomCode,

            MainGuestId = reservation.GuestId,
            GuestIds = guestIds,

            CheckedInAt = stay.CheckedInAt,
            CheckedInByAccountId = accountId,

            ReservationStatus =
                reservation.Status.ToString(),

            Message = "Check-in thành công."
        };
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-02: View Stays
    // UC-F07-07: Search & Filter Stays
    //
    // OData API:
    // https://localhost:7024/odata/StayViews
    //
    // Test danh sách:
    // /odata/StayViews?$top=10
    //
    // Test lọc:
    // /odata/StayViews?$filter=Status eq 'Active'
    //
    // Test tìm theo mã:
    // /odata/StayViews?$filter=contains(StayCode,'ST-')
    //
    // Test sắp xếp:
    // /odata/StayViews?$orderby=CheckedInAt desc
    //
    // Test phân trang:
    // /odata/StayViews?$skip=0&$top=10&$count=true
    // =================================================

    public IQueryable<StayListDto> GetStays()
    {
        // NhaNT | F07-02, F07-07
        // Repository tạo truy vấn; OData áp dụng bộ lọc.
        return _repository.QueryStayViews();
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-03: View Stay Detail
    // API: GET /api/stays/{id}
    //
    // Trả Stay, Reservation, Room, Guest và dịch vụ.
    // =================================================

    public async Task<StayDetailDto?> GetStayDetailAsync(
        int id)
    {
        // NhaNT | F07-03
        // Lấy thông tin chính của Stay.
        var stay =
            await _repository.GetStayDetailAsync(id);

        if (stay == null)
        {
            return null;
        }

        // NhaNT | F07-03
        // Lấy khách thực tế trong Stay.
        stay.Guests =
            await _repository.GetStayGuestsAsync(id);

        // NhaNT | F07-03
        // Chỉ lấy dịch vụ chưa bị xóa mềm.
        stay.ServiceUsages =
            await _repository.GetStayServiceUsagesAsync(id);

        return stay;
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-06: Check-out
    // API: POST /api/stays/{id}/check-out
    //
    // Chỉ trả phòng khi Invoice đã chốt và đủ tiền.
    // =================================================

    public async Task<CheckOutResponse> CheckOutAsync(
        int stayId,
        int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F07-06
        // Kiểm tra quyền Receptionist.
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null ||
            !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new InvoiceBusinessException(
                403,
                "Tài khoản không có quyền check-out.");
        }

        // NhaNT | F07-06
        // Stay phải tồn tại và chưa check-out.
        var stay =
            await _repository.GetStayForCheckOutAsync(
                stayId);

        if (stay == null)
        {
            throw new InvoiceBusinessException(
                404,
                "Không tìm thấy Stay.");
        }

        if (stay.CheckedOutAt != null)
        {
            throw new InvoiceBusinessException(
                409,
                "Stay đã check-out trước đó.");
        }

        // NhaNT | F07-06
        // Invoice bắt buộc đã Finalized.
        var invoice =
            await _repository.GetInvoiceByStayIdAsync(
                stayId);

        if (invoice == null ||
            invoice.Status != InvoiceStatus.Finalized)
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn chưa được Finalized.");
        }

        // NhaNT | F07-06
        // Kiểm tra tổng Payment bằng tổng Invoice.
        var totalPaid =
            await _repository.GetTotalPaidAsync(invoice.Id);

        if (totalPaid != invoice.TotalAmount)
        {
            throw new InvoiceBusinessException(
                409,
                $"Chưa thanh toán đủ. " +
                $"Tổng hóa đơn: {invoice.TotalAmount}, " +
                $"Đã thanh toán: {totalPaid}, " +
                $"Còn lại: {invoice.TotalAmount - totalPaid}.");
        }

        var now = DateTime.UtcNow;

        // NhaNT | F07-06
        // Kết thúc thời gian lưu trú.
        stay.CheckedOutAt = now;
        stay.CheckedOutByAccountId = accountId;

        // Hoàn tất Reservation.
        stay.Reservation.Status =
            ReservationStatus.Completed;

        // Phòng chuyển Dirty để chờ dọn.
        stay.Room.HousekeepingStatus =
            HousekeepingStatus.Dirty;

        // NhaNT | F07-06
        // Ghi AuditLog trong cùng transaction.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "CheckOut",
            EntityType = "Stay",
            EntityId = stay.Id.ToString(),
            EntityCodeSnapshot = stay.StayCode,
            OccurredAt = now,

            Summary =
                $"Check-out Stay {stay.StayCode}, " +
                $"phòng {stay.Room.RoomCode}.",

            NewValuesJson = JsonSerializer.Serialize(new
            {
                stay.CheckedOutAt,
                stay.CheckedOutByAccountId,
                ReservationStatus = "Completed",
                HousekeepingStatus = "Dirty",
                invoice.TotalAmount,
                TotalPaid = totalPaid
            })
        });

        // NhaNT | F07-06
        // Lưu và commit các cập nhật đồng bộ.
        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CheckOutResponse
        {
            StayId = stay.Id,
            StayCode = stay.StayCode,
            ReservationId = stay.ReservationId,
            RoomId = stay.RoomId,
            RoomCode = stay.Room.RoomCode,

            CheckedOutAt = now,
            CheckedOutByAccountId = accountId,

            InvoiceTotal = invoice.TotalAmount,
            TotalPaid = totalPaid,

            ReservationStatus = "Completed",
            HousekeepingStatus = "Dirty",

            Message = "Check-out thành công."
        };
    }
}

// =====================================================
// Developer: NhaNT
// UC-F07-01: Business Exception
// Trả HTTP status tương ứng lỗi nghiệp vụ.
// =====================================================

public class StayBusinessException : Exception
{
    public int StatusCode { get; }

    public StayBusinessException(
        int statusCode,
        string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
