
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Repositories.Implementations;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Layer: Repository
//
// Swagger: https://localhost:7024/swagger
//
// UC-F11-01: GET /api/reports/rooms
// UC-F11-02: GET /api/reports/reservations
// UC-F11-03: GET /api/reports/revenue
// UC-F11-04: GET /api/reports/tasks
//
// =====================================================

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;

    public ReportRepository(AppDbContext db)
    {
        _db = db;
    }

    // NhaNT | F11
    // Giữ cách lọc thời gian UTC từ code hiện tại.
    private static DateTime StartUtc(DateOnly date) =>
        DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

    private static DateTime EndExclusiveUtc(DateOnly date) =>
        StartUtc(date.AddDays(1));

    // NhaNT | F11
    // Đọc tài khoản để Controller kiểm tra quyền Admin.
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-01: Room Report
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/reports/rooms
    // Query: fromDate, toDate, roomTypeId (optional)
    //
    // Thống kê trạng thái hiện tại và phát sinh trong kỳ.
    // =================================================
    public async Task<RoomReportDto> GetRoomsAsync(
        RoomReportQuery query)
    {
        var start = StartUtc(query.FromDate);
        var end = EndExclusiveUtc(query.ToDate);
        var now = DateTime.UtcNow;

        var roomsQuery = _db.Rooms
            .AsNoTracking()
            .Where(r => r.DeletedAt == null);

        if (query.RoomTypeId.HasValue)
        {
            roomsQuery = roomsQuery.Where(r =>
                r.RoomTypeId == query.RoomTypeId.Value);
        }

        var rooms = await roomsQuery
            .Select(r => new
            {
                r.Id,
                r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                r.OperationalStatus,
                r.HousekeepingStatus
            })
            .ToListAsync();

        var roomIds = rooms
            .Select(r => r.Id)
            .ToArray();

        // NhaNT | F11-01
        // Phòng có khách thực tế đang lưu trú.
        var occupiedNowIds = await _db.Stays
            .AsNoTracking()
            .Where(s =>
                roomIds.Contains(s.RoomId) &&
                s.CheckedOutAt == null &&
                s.CheckedInAt <= now)
            .Select(s => s.RoomId)
            .Distinct()
            .ToListAsync();

        // NhaNT | F11-01
        // Các lượt Stay giao với kỳ báo cáo.
        var staysInPeriod = await _db.Stays
            .AsNoTracking()
            .Where(s =>
                roomIds.Contains(s.RoomId) &&
                s.CheckedInAt < end &&
                (s.CheckedOutAt == null ||
                 s.CheckedOutAt >= start))
            .Select(s => s.RoomId)
            .ToListAsync();

        // NhaNT | F11-01
        // Reservation dự kiến, bỏ Cancelled và NoShow.
        var roomsBookedInPeriod =
            await _db.Reservations
                .AsNoTracking()
                .Where(r =>
                    roomIds.Contains(r.RoomId) &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.Status != ReservationStatus.NoShow &&
                    r.CheckInDate <= query.ToDate &&
                    r.CheckOutDate > query.FromDate)
                .Select(r => r.RoomId)
                .Distinct()
                .ToListAsync();

        // NhaNT | F11-01
        // Phòng bị khóa và chưa được giải phóng.
        var blockedNowIds = await _db.RoomBlocks
            .AsNoTracking()
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.CancelledAt == null &&
                b.StartAt <= now &&
                (b.EndAt == null || b.EndAt > now) &&
                (b.ReleasedAt == null ||
                 b.ReleasedAt > now))
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        // NhaNT | F11-01
        // RoomBlock giao với kỳ báo cáo.
        var blockedPeriodIds = await _db.RoomBlocks
            .AsNoTracking()
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.CancelledAt == null &&
                b.StartAt < end &&
                (b.EndAt == null || b.EndAt > start) &&
                (b.ReleasedAt == null ||
                 b.ReleasedAt > start))
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        var occupiedSet = occupiedNowIds.ToHashSet();
        var blockedSet = blockedNowIds.ToHashSet();

        var activeRooms = rooms.Count(r =>
            r.OperationalStatus == RoomOperationalStatus.Active);

        var occupiedRooms = rooms.Count(r =>
            occupiedSet.Contains(r.Id));

        // NhaNT | F11-01
        // Vacant không đồng nghĩa đã vệ sinh xong.
        var vacantRooms = rooms.Count(r =>
            r.OperationalStatus ==
                RoomOperationalStatus.Active &&
            !occupiedSet.Contains(r.Id) &&
            !blockedSet.Contains(r.Id));

        return new RoomReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,

            TotalRooms = rooms.Count,
            ActiveRooms = activeRooms,
            OutOfServiceRooms =
                rooms.Count - activeRooms,

            CurrentlyOccupiedRooms = occupiedRooms,
            CurrentlyVacantRooms = vacantRooms,
            CurrentlyBlockedRooms = blockedSet.Count,

            RoomsWithStaysInPeriod =
                staysInPeriod.Distinct().Count(),

            StayCountInPeriod = staysInPeriod.Count,

            RoomsWithReservationsInPeriod =
                roomsBookedInPeriod.Count,

            RoomsBlockedInPeriod = blockedPeriodIds.Count,

            ByHousekeepingStatus = ReportMapper.ToCountItems(
                rooms.Select(r =>
                    r.HousekeepingStatus.ToString())),

            ByRoomType = ReportMapper.ToCountItems(
                rooms.Select(r =>
                    $"{r.RoomTypeId} - {r.RoomTypeName}"))
        };
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-02: Reservation Report
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/reports/reservations
    // Query: fromDate, toDate, status, roomTypeId
    //
    // Lọc theo CheckInDate của Reservation.
    // =================================================
    public async Task<ReservationReportDto> GetReservationsAsync(
        ReservationReportQuery query)
    {
        var reservationsQuery = _db.Reservations
            .AsNoTracking()
            .Where(r =>
                r.CheckInDate >= query.FromDate &&
                r.CheckInDate <= query.ToDate);

        if (query.Status.HasValue)
        {
            reservationsQuery = reservationsQuery
                .Where(r =>
                    r.Status == query.Status.Value);
        }

        if (query.RoomTypeId.HasValue)
        {
            reservationsQuery = reservationsQuery
                .Where(r =>
                    r.RoomTypeId == query.RoomTypeId.Value);
        }

        var records = await reservationsQuery
            .Select(r => new
            {
                r.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.NumberOfGuests,
                r.Status,
                r.RoomTypeId,
                RoomTypeName = r.RoomType.Name
            })
            .ToListAsync();

        return new ReservationReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            DateField = "CheckInDate",

            TotalReservations = records.Count,
            TotalGuests = records.Sum(r =>
                r.NumberOfGuests),

            TotalBookedNights = records.Sum(r =>
                Math.Max(
                    0,
                    r.CheckOutDate.DayNumber -
                    r.CheckInDate.DayNumber)),

            ByStatus = ReportMapper.ToCountItems(
                records.Select(r =>
                    r.Status.ToString())),

            ByRoomType = ReportMapper.ToCountItems(
                records.Select(r =>
                    $"{r.RoomTypeId} - {r.RoomTypeName}"))
        };
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-03: Revenue Report
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/reports/revenue
    // Query: fromDate, toDate, paymentMethod (optional)
    //
    // Doanh thu thực thu = SUM(Payment.Amount).
    // Không cộng thêm Invoice.TotalAmount.
    // =================================================
    public async Task<RevenueReportDto> GetRevenueAsync(
        RevenueReportQuery query)
    {
        var start = StartUtc(query.FromDate);
        var end = EndExclusiveUtc(query.ToDate);

        var paymentsQuery = _db.Payments
            .AsNoTracking()
            .Where(p =>
                p.PaidAt >= start &&
                p.PaidAt < end);

        if (query.PaymentMethod.HasValue)
        {
            paymentsQuery = paymentsQuery
                .Where(p =>
                    p.Method == query.PaymentMethod.Value);
        }

        // NhaNT | F11-03
        // GROUP BY PaymentMethod trực tiếp tại SQL Server.
        var groups = await paymentsQuery
            .GroupBy(p => p.Method)
            .Select(g => new
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(p => p.Amount)
            })
            .ToListAsync();

        var items = groups
            .Select(g => new RevenueMethodItem
            {
                Method = g.Method.ToString(),
                PaymentCount = g.Count,
                Amount = g.Amount
            })
            .OrderBy(g => g.Method)
            .ToList();

        return new RevenueReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            DateField = "PaidAt (UTC)",

            PaymentCount = items.Sum(x =>
                x.PaymentCount),

            TotalRevenue = items.Sum(x =>
                x.Amount),

            ByMethod = items
        };
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F11-04: Task Report
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/reports/tasks
    // Query: fromDate, toDate, dateField,
    //        taskType, status, assignedToAccountId
    //
    // dateField=created: lọc theo CreatedAt.
    // dateField=completed: lọc theo CompletedAt.
    // =================================================
    public async Task<TaskReportDto> GetTasksAsync(
        TaskReportQuery query)
    {
        var start = StartUtc(query.FromDate);
        var end = EndExclusiveUtc(query.ToDate);

        var tasksQuery = _db.HotelTasks
            .AsNoTracking()
            .AsQueryable();

        var useCompletedDate = query.DateField.Equals(
            "completed",
            StringComparison.OrdinalIgnoreCase);

        if (useCompletedDate)
        {
            tasksQuery = tasksQuery.Where(t =>
                t.CompletedAt != null &&
                t.CompletedAt >= start &&
                t.CompletedAt < end);
        }
        else
        {
            tasksQuery = tasksQuery.Where(t =>
                t.CreatedAt >= start &&
                t.CreatedAt < end);
        }

        if (query.TaskType.HasValue)
        {
            tasksQuery = tasksQuery.Where(t =>
                t.TaskType == query.TaskType.Value);
        }

        if (query.Status.HasValue)
        {
            tasksQuery = tasksQuery.Where(t =>
                t.Status == query.Status.Value);
        }

        if (query.AssignedToAccountId.HasValue)
        {
            tasksQuery = tasksQuery.Where(t =>
                t.AssignedToAccountId ==
                query.AssignedToAccountId.Value);
        }

        // NhaNT | F11-04
        // Nhóm theo loại Task và trạng thái tại SQL.
        var groups = await tasksQuery
            .GroupBy(t => new
            {
                t.TaskType,
                t.Status
            })
            .Select(g => new
            {
                g.Key.TaskType,
                g.Key.Status,
                Count = g.Count(),
                AssignedCount = g.Count(t =>
                    t.AssignedToAccountId != null)
            })
            .ToListAsync();

        var total = groups.Sum(g => g.Count);

        var completed = groups
            .Where(g =>
                g.Status == HotelTaskStatus.Completed)
            .Sum(g => g.Count);

        return new TaskReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,

            DateField = useCompletedDate
                ? "CompletedAt"
                : "CreatedAt",

            TotalTasks = total,
            CompletedTasks = completed,
            UncompletedTasks = total - completed,

            AssignedTasks = groups.Sum(g =>
                g.AssignedCount),

            ByTaskType = ReportMapper.ToCountItems(
                groups.Select(g => (
                    Key: g.TaskType.ToString(),
                    Count: g.Count))),

            ByStatus = ReportMapper.ToCountItems(
                groups.Select(g => (
                    Key: g.Status.ToString(),
                    Count: g.Count)))
        };
    }
}
