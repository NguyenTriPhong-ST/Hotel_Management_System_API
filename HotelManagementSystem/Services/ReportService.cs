
using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Layer: Service
//
// Swagger: https://localhost:7024/swagger
//
// UC-F11-01: GET /api/reports/rooms
// UC-F11-02: GET /api/reports/reservations
// UC-F11-03: GET /api/reports/revenue
// UC-F11-04: GET /api/reports/tasks
//
// =====================================================

public class ReportService : IReportService
{
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository)
    {
        _repository = repository;
    }

    // NhaNT | F11
    // Lấy tài khoản để Controller kiểm tra quyền Admin.
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _repository.GetAccountAsync(accountId);
    }

    // NhaNT | UC-F11-01
    // Thống kê phòng hiện tại và phát sinh trong kỳ.
    public Task<RoomReportDto> GetRoomsAsync(
        RoomReportQuery query)
    {
        return _repository.GetRoomsAsync(query);
    }

    // NhaNT | UC-F11-02
    // Thống kê đặt phòng theo CheckInDate.
    public Task<ReservationReportDto> GetReservationsAsync(
        ReservationReportQuery query)
    {
        return _repository.GetReservationsAsync(query);
    }

    // NhaNT | UC-F11-03
    // Thống kê doanh thu thực thu từ Payment.
    public Task<RevenueReportDto> GetRevenueAsync(
        RevenueReportQuery query)
    {
        return _repository.GetRevenueAsync(query);
    }

    // NhaNT | UC-F11-04
    // Thống kê công việc theo CreatedAt/CompletedAt.
    public Task<TaskReportDto> GetTasksAsync(
        TaskReportQuery query)
    {
        return _repository.GetTasksAsync(query);
    }
}
