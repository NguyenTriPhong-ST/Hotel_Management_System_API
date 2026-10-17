
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Implementations;

// =====================================================
// Developer: NhaNT
// Module: F07 - Stay Management
// Layer: Repository
//
// UC-F07-01: Kiểm tra điều kiện nhận phòng.
// UC-F07-02: Truy vấn danh sách Stay.
// UC-F07-03: Truy vấn chi tiết Stay.
// UC-F07-06: Cập nhật dữ liệu trả phòng.
// UC-F07-07: Truy vấn lọc và tìm kiếm.
//
// OData test:
// http://localhost:5142/odata/StayViews?$top=10
// http://localhost:5142/odata/StayViews?$filter=Status%20eq%20'Active'
// http://localhost:5142/odata/StayViews?$orderby=Id%20desc
//
// Repository chỉ truy cập dữ liệu bằng EF Core.
// =====================================================

public class StayRepository : IStayRepository
{
    private readonly AppDbContext _db;

    public StayRepository(AppDbContext db)
    {
        _db = db;
    }

    // =================================================
    // NhaNT | F07-01, F07-06
    // Mở transaction để dữ liệu được lưu đồng bộ.
    // =================================================
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
    }

    // =================================================
    // NhaNT | F07-01, F07-06
    // Lấy tài khoản nhân viên đang thao tác.
    // =================================================
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    // =================================================
    // NhaNT | F07-01 - Check-in
    // API: POST /api/stays/check-in
    // Lấy Reservation cùng Room và Guest.
    // =================================================
    public Task<Reservation?> GetReservationForCheckInAsync(
        int reservationId)
    {
        return _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(r => r.Id == reservationId);
    }

    // NhaNT | F07-01
    // Một phòng chỉ được có một Stay đang hoạt động.
    public Task<bool> HasActiveStayAsync(int roomId)
    {
        return _db.Stays.AnyAsync(s =>
            s.RoomId == roomId &&
            s.CheckedOutAt == null);
    }

    // NhaNT | F07-01
    // Không cho check-in nếu RoomBlock còn hiệu lực.
    public Task<bool> IsRoomBlockedAsync(
        int roomId, DateTime now)
    {
        return _db.RoomBlocks.AnyAsync(b =>
            b.RoomId == roomId &&
            b.CancelledAt == null &&
            b.StartAt <= now &&
            (b.EndAt == null || b.EndAt > now) &&
            (b.ReleasedAt == null || b.ReleasedAt > now));
    }

    // NhaNT | F07-01
    // Kiểm tra khách tồn tại và chưa xóa mềm.
    public Task<int> CountValidGuestsAsync(
        List<int> guestIds)
    {
        return _db.Guests.CountAsync(g =>
            guestIds.Contains(g.Id) &&
            g.DeletedAt == null);
    }

    // NhaNT | F07-01
    // Kiểm tra Reservation đã phát sinh Stay.
    public Task<bool> HasStayForReservationAsync(
        int reservationId)
    {
        return _db.Stays.AnyAsync(s =>
            s.ReservationId == reservationId);
    }

    // NhaNT | F07-01
    // Thêm Stay; SaveChanges thực hiện trong Service.
    public void AddStay(Stay stay)
    {
        _db.Stays.Add(stay);
    }

    // =================================================
    // NhaNT | F07-02, F07-07 - View/Search Stays
    //
    // API: GET /odata/StayViews
    //
    // Test:
    // /odata/StayViews?$filter=Status eq 'Active'
    // /odata/StayViews?$orderby=CheckedInAt desc
    // /odata/StayViews?$skip=0&$top=10
    // /odata/StayViews?$count=true
    //
    // IQueryable giúp OData truy vấn trên SQL Server.
    // =================================================
    public IQueryable<StayListDto> QueryStayViews()
    {
        return _db.Stays
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
                MainGuestName =
                    s.Reservation.Guest.FullName,

                CheckedInAt = s.CheckedInAt,
                CheckedOutAt = s.CheckedOutAt,

                Status = s.CheckedOutAt == null
                    ? "Active"
                    : "Completed"
            });
    }

    // =================================================
    // NhaNT | F07-03 - View Stay Detail
    // API: GET /api/stays/{id}
    // =================================================
    public Task<StayDetailDto?> GetStayDetailAsync(
        int stayId)
    {
        return _db.Stays
            .AsNoTracking()
            .Where(s => s.Id == stayId)
            .Select(s => new StayDetailDto
            {
                Id = s.Id,
                StayCode = s.StayCode,

                ReservationId = s.ReservationId,
                ReservationCode =
                    s.Reservation.ReservationCode,

                CheckInDate =
                    s.Reservation.CheckInDate,
                CheckOutDate =
                    s.Reservation.CheckOutDate,
                NumberOfGuests =
                    s.Reservation.NumberOfGuests,
                NightlyRate =
                    s.Reservation.NightlyRate,

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
            })
            .FirstOrDefaultAsync();
    }

    // NhaNT | F07-03
    // Lấy khách thực tế từ StayGuests.
    public Task<List<StayGuestDetailDto>> GetStayGuestsAsync(
        int stayId)
    {
        return (
            from sg in _db.StayGuests.AsNoTracking()
            join g in _db.Guests.AsNoTracking()
                on sg.GuestId equals g.Id
            where sg.StayId == stayId
            orderby sg.IsMainGuest descending, sg.GuestId
            select new StayGuestDetailDto
            {
                GuestId = sg.GuestId,
                FullName = g.FullName,
                IsMainGuest = sg.IsMainGuest
            }
        ).ToListAsync();
    }

    // NhaNT | F07-03
    // Lấy chi phí dịch vụ theo giá snapshot.
    public Task<List<StayServiceUsageDetailDto>>
        GetStayServiceUsagesAsync(int stayId)
    {
        return _db.ServiceUsages
            .AsNoTracking()
            .Where(su =>
                su.StayId == stayId &&
                su.DeletedAt == null)
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
    }

    // =================================================
    // NhaNT | F07-06 - Check-out
    // API: POST /api/stays/{id}/check-out
    // Lấy các Entity cần cập nhật trạng thái.
    // =================================================
    public Task<Stay?> GetStayForCheckOutAsync(int stayId)
    {
        return _db.Stays
            .Include(s => s.Reservation)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);
    }

    // NhaNT | F07-06
    // Lấy hóa đơn để kiểm tra Finalized.
    public Task<Invoice?> GetInvoiceByStayIdAsync(
        int stayId)
    {
        return _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);
    }

    // NhaNT | F07-06
    // Tổng tiền đã thanh toán cho Invoice.
    public async Task<decimal> GetTotalPaidAsync(
        int invoiceId)
    {
        return await _db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
    }

    // NhaNT | F07-01, F07-06
    // Ghi AuditLog trong transaction hiện tại.
    public void AddAuditLog(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
    }

    // NhaNT | F07-01, F07-06
    // Lưu toàn bộ thay đổi thông qua DbContext.
    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
