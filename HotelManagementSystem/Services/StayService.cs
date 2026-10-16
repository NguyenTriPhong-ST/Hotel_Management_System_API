
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F07-01 - Check-in
// Mô tả: Xử lý nhận phòng, kiểm tra điều kiện,
// tạo Stay, StayGuests và cập nhật Reservation.
// OData: Không dùng cho thao tác ghi dữ liệu.
// Lưu ý về JWT: Do module F01 của nhóm chưa được tích hợp, chúng ta sẽ dùng header X-Demo-Account-Id chỉ trong môi trường Development để kiểm thử. Đây là cơ chế tạm thời, không dùng trên môi trường thật. Khi F01 hoàn thành, sẽ thay bằng Account ID lấy từ JWT.
// Do UC đang code dơn lẽ chưa phối hợp với UC F01 nên tạm thời dùng header X-Demo-Account-Id để kiểm thử. Khi F01 hoàn thành, sẽ thay bằng Account ID lấy từ JWT. Sau khi F01 hoàn thành, sẽ thay bằng Account ID lấy từ JWT. Sau khi F01 hoàn thành, sẽ thay bằng Account ID lấy từ JWT.
// =====================================================

public class StayService
{
    private readonly AppDbContext _db;

    public StayService(AppDbContext db)
    {
        _db = db;
    }

    // NhaNTCE181298 | UC-F07-01
    // Thực hiện toàn bộ nghiệp vụ check-in.
    public async Task<CheckInResponse> CheckInAsync(
        CheckInRequest request,
        int accountId)
    {
        // NhaNTCE181298 | UC-F07-01
        // Transaction Serializable hạn chế xung đột
        // khi nhiều request cùng thao tác check-in.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        // Bước 1: Kiểm tra nhân viên thực hiện.
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null ||
            !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new StayBusinessException(
                403,
                "Tài khoản không có quyền check-in.");
        }

        // Bước 2: Kiểm tra Reservation.
        var reservation = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(
                r => r.Id == request.ReservationId);

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

        // Bước 3: Kiểm tra ngày check-in.
        // Ngày nghiệp vụ sử dụng múi giờ Việt Nam.
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

        // Bước 4: Kiểm tra phòng.
        var room = reservation.Room;

        if (room.DeletedAt != null ||
            room.OperationalStatus != RoomOperationalStatus.Active)
        {
            throw new StayBusinessException(
                409,
                "Phòng không còn hoạt động.");
        }

        if (room.HousekeepingStatus != HousekeepingStatus.Inspected)
        {
            throw new StayBusinessException(
                409,
                "Phòng chưa đạt trạng thái Inspected.");
        }

        // NhaNTCE181298 | UC-F07-01
        // Một phòng không được có hai Stay đang hoạt động.
        var hasActiveStay = await _db.Stays
            .AnyAsync(s =>
                s.RoomId == room.Id &&
                s.CheckedOutAt == null);

        if (hasActiveStay)
        {
            throw new StayBusinessException(
                409,
                "Phòng đang có khách lưu trú.");
        }

        // Bước 5: Kiểm tra RoomBlock còn hiệu lực.
        var isBlocked = await _db.RoomBlocks
            .AnyAsync(b =>
                b.RoomId == room.Id &&
                b.CancelledAt == null &&
                b.StartAt <= now &&
                (b.EndAt == null || b.EndAt > now) &&
                (b.ReleasedAt == null || b.ReleasedAt > now));

        if (isBlocked)
        {
            throw new StayBusinessException(
                409,
                "Phòng đang bị khóa hoặc bảo trì.");
        }

        // Bước 6: Kiểm tra danh sách khách.
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

        // Khách đại diện phải có trong danh sách nhận phòng.
        if (!guestIds.Contains(reservation.GuestId))
        {
            throw new StayBusinessException(
                400,
                "Thiếu khách đại diện của Reservation.");
        }

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

        var validGuestCount = await _db.Guests
            .CountAsync(g =>
                guestIds.Contains(g.Id) &&
                g.DeletedAt == null);

        if (validGuestCount != guestIds.Count)
        {
            throw new StayBusinessException(
                400,
                "Có khách không tồn tại hoặc đã bị lưu trữ.");
        }

        // Bước 7: Chống check-in lặp.
        var alreadyCheckedIn = await _db.Stays
            .AnyAsync(s =>
                s.ReservationId == reservation.Id);

        if (alreadyCheckedIn)
        {
            throw new StayBusinessException(
                409,
                "Reservation đã được check-in trước đó.");
        }

        // Bước 8: Tạo Stay và mã lưu trú duy nhất.
        var stay = new Stay
        {
            StayCode = $"ST-{Guid.NewGuid():N}"[..19],
            ReservationId = reservation.Id,
            RoomId = room.Id,
            CheckedInAt = now,
            CheckedInByAccountId = accountId,
            Notes = request.Notes
        };

        // Bước 9: Thêm khách thực tế vào StayGuests.
        foreach (var guestId in guestIds)
        {
            stay.StayGuests.Add(new StayGuest
            {
                GuestId = guestId,
                IsMainGuest = guestId == reservation.GuestId,
                AddedAt = now
            });
        }

        _db.Stays.Add(stay);

        // Bước 10: Cập nhật trạng thái Reservation.
        reservation.Status = ReservationStatus.CheckedIn;

        // Lưu Stay trước để nhận StayId.
        await _db.SaveChangesAsync();

        // Bước 11: Ghi AuditLog cùng transaction.
        // Khi nhóm tích hợp LOG-01, cần dùng
        // cơ chế Audit chung thay cho đoạn này
        // để tránh ghi log hai lần.
        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "CheckIn",
            EntityType = "Stay",
            EntityId = stay.Id.ToString(),
            EntityCodeSnapshot = stay.StayCode,
            OccurredAt = now,
            Summary =
                $"Check-in Reservation {reservation.ReservationCode} " +
                $"vào phòng {room.RoomCode}.",
            CorrelationId = Guid.NewGuid().ToString()
        });

        await _db.SaveChangesAsync();

        // Bước 12: Commit toàn bộ giao dịch.
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

            ReservationStatus = reservation.Status.ToString(),
            Message = "Check-in thành công."
        };



    }

    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F07-02 - View Stays
    // OData: Trả IQueryable để hỗ trợ truy vấn động.
    // Mô tả: Lấy danh sách lưu trú cùng Reservation,
    // phòng và khách đại diện.
    // =====================================================

    public IQueryable<StayListDto> GetStays()
    {
        // NhaNTCE181298 | UC-F07-02 | OData
        // AsNoTracking tối ưu hiệu năng cho API chỉ đọc.
        // Select chuyển Entity thành DTO trước khi trả về.
        var query = _db.Stays
            .AsNoTracking()
            .Select(s => new StayListDto
            {
                Id = s.Id,
                StayCode = s.StayCode,

                ReservationId = s.ReservationId,
                ReservationCode =
                    s.Reservation.ReservationCode,

                RoomId = s.RoomId,
                RoomCode = s.Room.RoomCode,

                MainGuestId = s.Reservation.GuestId,
                MainGuestName = s.Reservation.Guest.FullName,

                CheckedInAt = s.CheckedInAt,
                CheckedOutAt = s.CheckedOutAt,

                Status = s.CheckedOutAt == null
                    ? "Active"
                    : "Completed"
            });

        // NhaNTCE181298 | UC-F07-02 | OData
        // Không gọi ToList() ở đây.
        // Giữ IQueryable để OData xử lý filter,
        // orderby, top, skip trực tiếp trên SQL Server.
        return query;
    }



    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F07-03 - View Stay Details
    // Mô tả: Truy vấn một Stay và dữ liệu liên quan.
    // OData: Không dùng, vì lấy chi tiết theo Stay ID.
    // =====================================================

    public async Task<StayDetailDto?> GetStayDetailAsync(int id)
    {
        // NhaNTCE181298 | UC-F07-03
        // Lấy Stay kết hợp Reservation và Room.
        var stay = await _db.Stays
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StayDetailDto
            {
                Id = s.Id,
                StayCode = s.StayCode,

                ReservationId = s.ReservationId,
                ReservationCode = s.Reservation.ReservationCode,
                CheckInDate = s.Reservation.CheckInDate,
                CheckOutDate = s.Reservation.CheckOutDate,
                NumberOfGuests = s.Reservation.NumberOfGuests,
                NightlyRate = s.Reservation.NightlyRate,

                RoomId = s.RoomId,
                RoomCode = s.Room.RoomCode,

                CheckedInAt = s.CheckedInAt,
                CheckedOutAt = s.CheckedOutAt,
                CheckedInByAccountId = s.CheckedInByAccountId,
                CheckedOutByAccountId = s.CheckedOutByAccountId,
                Notes = s.Notes,

                Status = s.CheckedOutAt == null
                    ? "Active"
                    : "Completed"
            })
            .FirstOrDefaultAsync();

        // Không tìm thấy Stay.
        if (stay == null)
        {
            return null;
        }

        // NhaNTCE181298 | UC-F07-03
        // Lấy danh sách khách thực tế của Stay.
        stay.Guests = await (
            from sg in _db.StayGuests.AsNoTracking()
            join g in _db.Guests.AsNoTracking()
                on sg.GuestId equals g.Id
            where sg.StayId == id
            orderby sg.IsMainGuest descending, sg.GuestId
            select new StayGuestDetailDto
            {
                GuestId = sg.GuestId,
                FullName = g.FullName,
                IsMainGuest = sg.IsMainGuest
            }
        ).ToListAsync();

        // NhaNTCE181298 | UC-F07-03
        // Lấy dịch vụ chưa bị soft-delete.
        // Giá dùng snapshot đã lưu tại thời điểm sử dụng.
        stay.ServiceUsages = await _db.ServiceUsages
            .AsNoTracking()
            .Where(su => su.StayId == id && su.DeletedAt == null)
            .OrderBy(su => su.UsedAt)
            .Select(su => new StayServiceUsageDetailDto
            {
                Id = su.Id,
                HotelServiceId = su.HotelServiceId,
                ServiceName = su.ServiceNameSnapshot,
                Unit = su.UnitSnapshot,
                Quantity = su.Quantity,
                UnitPrice = su.UnitPrice,
                Amount = su.Quantity * su.UnitPrice,
                UsedAt = su.UsedAt
            })
            .ToListAsync();

        return stay;
    }

    // =====================================================
    // Developer: NhaNTCE181298
    // UC-F07-06: Check-out
    // Endpoint: POST /api/stays/{id}/check-out
    // OData: Không dùng cho POST.
    // =====================================================

    public async Task<CheckOutResponse> CheckOutAsync(
        int stayId, int accountId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null || !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new InvoiceBusinessException(
                403, "Tài khoản không có quyền check-out.");
        }

        var stay = await _db.Stays
            .Include(s => s.Reservation)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay == null)
            throw new InvoiceBusinessException(
                404, "Không tìm thấy Stay.");

        if (stay.CheckedOutAt != null)
            throw new InvoiceBusinessException(
                409, "Stay đã check-out trước đó.");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);

        if (invoice == null ||
            invoice.Status != InvoiceStatus.Finalized)
        {
            throw new InvoiceBusinessException(
                409, "Hóa đơn chưa được Finalized.");
        }

        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

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

        // Hoàn tất Stay.
        stay.CheckedOutAt = now;
        stay.CheckedOutByAccountId = accountId;

        // Hoàn tất Reservation.
        stay.Reservation.Status = ReservationStatus.Completed;

        // Phòng cần được dọn trước khi đón khách mới.
        stay.Room.HousekeepingStatus = HousekeepingStatus.Dirty;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "CheckOut",
            EntityType = "Stay",
            EntityId = stay.Id.ToString(),
            EntityCodeSnapshot = stay.StayCode,
            OccurredAt = now,
            Summary = $"Check-out Stay {stay.StayCode}, " +
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

        await _db.SaveChangesAsync();
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
// Developer: NhaNTCE181298
// UC: UC-F07-01
// Mô tả: Exception nghiệp vụ kèm HTTP status code.
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
