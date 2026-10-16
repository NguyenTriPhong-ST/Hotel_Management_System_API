
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Reports;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
//
// UC-F11-01: Room Report
// UC-F11-02: Reservation Report
// UC-F11-03: Revenue Report
// UC-F11-04: Task Report
//
// Technology: EF Core + LINQ + SQL Server
// Tất cả là API chỉ đọc.
// =====================================================

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    private static DateTime StartUtc(DateOnly date) =>
        DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

    private static DateTime EndExclusiveUtc(DateOnly date) =>
        StartUtc(date.AddDays(1));

    // =================================================
    // UC-F11-01 - ROOM REPORT
    // GET /api/reports/rooms
    //
    // Trạng thái hiện tại tách riêng khỏi phát sinh kỳ.
    // Stay: thực tế CheckedInAt/CheckedOutAt.
    // Reservation: ngày đặt dự kiến.
    // RoomBlock: chỉ tính khóa còn hiệu lực.
    // =================================================

    public async Task<RoomReportDto> GetRoomsAsync(
        RoomReportQuery query)
    {
        var start = StartUtc(query.FromDate);
        var end = EndExclusiveUtc(query.ToDate);
        var now = DateTime.UtcNow;

        var roomsQuery = _db.Rooms.AsNoTracking()
            .Where(r => r.DeletedAt == null);

        if (query.RoomTypeId.HasValue)
            roomsQuery = roomsQuery.Where(r =>
                r.RoomTypeId == query.RoomTypeId.Value);

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

        var roomIds = rooms.Select(r => r.Id).ToArray();

        // Stay đang ở thực tế, không phải tình trạng vệ sinh.
        var occupiedNowIds = await _db.Stays
            .AsNoTracking()
            .Where(s =>
                roomIds.Contains(s.RoomId) &&
                s.CheckedOutAt == null &&
                s.CheckedInAt <= now)
            .Select(s => s.RoomId)
            .Distinct()
            .ToListAsync();

        // Stay có giao với khoảng thời gian báo cáo.
        var staysInPeriod = await _db.Stays
            .AsNoTracking()
            .Where(s =>
                roomIds.Contains(s.RoomId) &&
                s.CheckedInAt < end &&
                (s.CheckedOutAt == null ||
                 s.CheckedOutAt >= start))
            .Select(s => s.RoomId)
            .ToListAsync();

        // Reservation dự kiến giao với kỳ.
        // Không tính đặt phòng Cancelled / NoShow.
        var roomsBookedInPeriod = await _db.Reservations
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

        // Block hiệu lực là khoảng [StartAt, min(EndAt,
        // ReleasedAt)), bỏ qua Block đã Cancelled.
        var blockedNowIds = await _db.RoomBlocks
            .AsNoTracking()
            .Where(b =>
                roomIds.Contains(b.RoomId) &&
                b.CancelledAt == null &&
                b.StartAt <= now &&
                (b.EndAt == null || b.EndAt > now) &&
                (b.ReleasedAt == null || b.ReleasedAt > now))
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

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

        var activeRooms = rooms
            .Count(r => r.OperationalStatus ==
                RoomOperationalStatus.Active);

        var occupiedRooms = rooms
            .Count(r => occupiedSet.Contains(r.Id));

        // Vacant = không có khách đang ở, không bị Block
        // và OperationalStatus vẫn Active.
        // Dirty không đồng nghĩa phòng đã sẵn sàng.
        var vacantRooms = rooms.Count(r =>
            r.OperationalStatus == RoomOperationalStatus.Active &&
            !occupiedSet.Contains(r.Id) &&
            !blockedSet.Contains(r.Id));

        return new RoomReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            TotalRooms = rooms.Count,
            ActiveRooms = activeRooms,
            OutOfServiceRooms = rooms.Count - activeRooms,
            CurrentlyOccupiedRooms = occupiedRooms,
            CurrentlyVacantRooms = vacantRooms,
            CurrentlyBlockedRooms = blockedSet.Count,

            RoomsWithStaysInPeriod =
                staysInPeriod.Distinct().Count(),
            StayCountInPeriod = staysInPeriod.Count,
            RoomsWithReservationsInPeriod =
                roomsBookedInPeriod.Count,
            RoomsBlockedInPeriod = blockedPeriodIds.Count,

            ByHousekeepingStatus = rooms
                .GroupBy(r => r.HousekeepingStatus)
                .Select(g => new ReportCountItem
                {
                    Key = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(x => x.Key)
                .ToList(),

            ByRoomType = rooms
                .GroupBy(r => new
                {
                    r.RoomTypeId,
                    r.RoomTypeName
                })
                .Select(g => new ReportCountItem
                {
                    Key = $"{g.Key.RoomTypeId} - " +
                          g.Key.RoomTypeName,
                    Count = g.Count()
                })
                .OrderBy(x => x.Key)
                .ToList()
        };
    }

    // =================================================
    // UC-F11-02 - RESERVATION REPORT
    // GET /api/reports/reservations
    //
    // Ngày lọc: CheckInDate (ngày nhận phòng dự kiến).
    // Cho phép lọc trạng thái và loại phòng.
    // =================================================

    public async Task<ReservationReportDto>
        GetReservationsAsync(ReservationReportQuery query)
    {
        var reservationsQuery = _db.Reservations
            .AsNoTracking()
            .Where(r =>
                r.CheckInDate >= query.FromDate &&
                r.CheckInDate <= query.ToDate);

        if (query.Status.HasValue)
            reservationsQuery = reservationsQuery
                .Where(r => r.Status == query.Status.Value);

        if (query.RoomTypeId.HasValue)
            reservationsQuery = reservationsQuery
                .Where(r =>
                    r.RoomTypeId == query.RoomTypeId.Value);

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
            TotalGuests = records
                .Sum(r => r.NumberOfGuests),
            TotalBookedNights = records.Sum(r =>
                Math.Max(0,
                    r.CheckOutDate.DayNumber -
                    r.CheckInDate.DayNumber)),

            ByStatus = records
                .GroupBy(r => r.Status)
                .Select(g => new ReportCountItem
                {
                    Key = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(x => x.Key)
                .ToList(),

            ByRoomType = records
                .GroupBy(r => new
                {
                    r.RoomTypeId,
                    r.RoomTypeName
                })
                .Select(g => new ReportCountItem
                {
                    Key = $"{g.Key.RoomTypeId} - " +
                          g.Key.RoomTypeName,
                    Count = g.Count()
                })
                .OrderBy(x => x.Key)
                .ToList()
        };
    }

    // =================================================
    // UC-F11-03 - REVENUE REPORT
    // GET /api/reports/revenue
    //
    // Doanh thu thực thu = SUM(Payments.Amount).
    // KHÔNG cộng Invoice.TotalAmount lần nữa.
    // Ngày lọc: Payments.PaidAt (UTC).
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
            paymentsQuery = paymentsQuery.Where(p =>
                p.Method == query.PaymentMethod.Value);

        // Tổng hợp bằng SQL thay vì tải toàn bộ Payment.
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
            PaymentCount = items.Sum(x => x.PaymentCount),
            TotalRevenue = items.Sum(x => x.Amount),
            ByMethod = items
        };
    }

    // =================================================
    // UC-F11-04 - TASK REPORT
    // GET /api/reports/tasks
    //
    // dateField=created: lọc theo CreatedAt.
    // dateField=completed: lọc theo CompletedAt.
    // Cho phép lọc loại task, status, người được giao.
    // =================================================

    public async Task<TaskReportDto> GetTasksAsync(
        TaskReportQuery query)
    {
        var start = StartUtc(query.FromDate);
        var end = EndExclusiveUtc(query.ToDate);

        var tasksQuery = _db.HotelTasks
            .AsNoTracking()
            .AsQueryable();

        if (query.DateField.Equals(
            "completed", StringComparison.OrdinalIgnoreCase))
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
            tasksQuery = tasksQuery.Where(t =>
                t.TaskType == query.TaskType.Value);

        if (query.Status.HasValue)
            tasksQuery = tasksQuery.Where(t =>
                t.Status == query.Status.Value);

        if (query.AssignedToAccountId.HasValue)
            tasksQuery = tasksQuery.Where(t =>
                t.AssignedToAccountId ==
                query.AssignedToAccountId.Value);

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
            DateField = query.DateField
                .Equals("completed",
                    StringComparison.OrdinalIgnoreCase)
                ? "CompletedAt" : "CreatedAt",
            TotalTasks = total,
            CompletedTasks = completed,
            UncompletedTasks = total - completed,
            AssignedTasks = groups.Sum(g => g.AssignedCount),

            ByTaskType = groups
                .GroupBy(g => g.TaskType)
                .Select(g => new ReportCountItem
                {
                    Key = g.Key.ToString(),
                    Count = g.Sum(x => x.Count)
                })
                .OrderBy(x => x.Key)
                .ToList(),

            ByStatus = groups
                .GroupBy(g => g.Status)
                .Select(g => new ReportCountItem
                {
                    Key = g.Key.ToString(),
                    Count = g.Sum(x => x.Count)
                })
                .OrderBy(x => x.Key)
                .ToList()
        };
    }
}
