using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Services.Interfaces;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Layer: Service Interface
//
// UC-F11-01: Room Report
// UC-F11-02: Reservation Report
// UC-F11-03: Revenue Report
// UC-F11-04: Task Report
// =====================================================

public interface IReportService
{
    Task<Account?> GetAccountAsync(int accountId);

    Task<RoomReportDto> GetRoomsAsync(
        RoomReportQuery query);

    Task<ReservationReportDto> GetReservationsAsync(
        ReservationReportQuery query);

    Task<RevenueReportDto> GetRevenueAsync(
        RevenueReportQuery query);

    Task<TaskReportDto> GetTasksAsync(
        TaskReportQuery query);
}