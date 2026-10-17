
using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Layer: Repository Interface
//
// Swagger: https://localhost:7024/swagger
// GET /api/reports/rooms
// GET /api/reports/reservations
// GET /api/reports/revenue
// GET /api/reports/tasks
//
// Chỉ truy vấn dữ liệu, không thay đổi Database.
// =====================================================

public interface IReportRepository
{
    // NhaNT | UC-F11-01: Room Report.
    Task<RoomReportDto> GetRoomsAsync(
        RoomReportQuery query);

    // NhaNT | UC-F11-02: Reservation Report.
    Task<ReservationReportDto> GetReservationsAsync(
        ReservationReportQuery query);

    // NhaNT | UC-F11-03: Revenue Report.
    Task<RevenueReportDto> GetRevenueAsync(
        RevenueReportQuery query);

    // NhaNT | UC-F11-04: Task Report.
    Task<TaskReportDto> GetTasksAsync(
        TaskReportQuery query);

    // NhaNT | UC-F11-01 đến UC-F11-04.
    // Kiểm tra tài khoản Admin cho Controller.
    Task<Account?> GetAccountAsync(int accountId);
}
