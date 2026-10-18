
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Interfaces;

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
//
// Repository chỉ truy cập dữ liệu bằng EF Core.
// =====================================================
public interface IStayRepository
{
    // NhaNT | F07-01, F07-06
    // Transaction chung cho Stay, Reservation và AuditLog.
    Task<IDbContextTransaction> BeginTransactionAsync();

    // NhaNT | F07-01, F07-06
    // Lấy tài khoản để kiểm tra quyền Receptionist.
    Task<Account?> GetAccountAsync(int accountId);

    // NhaNT | F07-01
    // Lấy đặt phòng cùng thông tin phòng và khách.
    Task<Reservation?> GetReservationForCheckInAsync(
        int reservationId);

    // NhaNT | F07-01
    // Kiểm tra phòng đã có khách nhận hay chưa.
    Task<bool> HasActiveStayAsync(int roomId);

    // NhaNT | F07-01
    // Kiểm tra phòng bị khóa hoặc bảo trì.
    Task<bool> IsRoomBlockedAsync(
        int roomId, DateTime now);

    // NhaNT | F07-01
    // Đếm số khách còn hiệu lực trong Database.
    Task<int> CountValidGuestsAsync(List<int> guestIds);

    // NhaNT | F07-01
    // Chống check-in trùng Reservation.
    Task<bool> HasStayForReservationAsync(
        int reservationId);

    // NhaNT | F07-01
    // Thêm Stay và StayGuests thông qua navigation.
    void AddStay(Stay stay);

    // NhaNT | F07-02, F07-07
    IQueryable<StayListDto> QueryStayViews();

    // NhaNT | F07-03
    // Lấy thông tin chi tiết Stay.
    Task<StayDetailDto?> GetStayDetailAsync(int stayId);

    // NhaNT | F07-03
    // Lấy danh sách khách thực tế nhận phòng.
    Task<List<StayGuestDetailDto>> GetStayGuestsAsync(
        int stayId);

    // NhaNT | F07-03
    // Lấy dịch vụ chưa bị xóa mềm.
    Task<List<StayServiceUsageDetailDto>>
        GetStayServiceUsagesAsync(int stayId);

    // NhaNT | F07-06
    // Lấy Stay, Reservation, Room để check-out.
    Task<Stay?> GetStayForCheckOutAsync(int stayId);

    // NhaNT | F07-06
    // Lấy hóa đơn của Stay.
    Task<Invoice?> GetInvoiceByStayIdAsync(int stayId);

    // NhaNT | F07-06
    // Tổng số tiền đã thanh toán.
    Task<decimal> GetTotalPaidAsync(int invoiceId);

    // NhaNT | F07-01, F07-06
    // Thêm nhật ký nghiệp vụ.
    void AddAuditLog(AuditLog auditLog);

    // NhaNT | F07-01, F07-06
    // Lưu thay đổi bằng EF Core.
    Task<int> SaveChangesAsync();
}
