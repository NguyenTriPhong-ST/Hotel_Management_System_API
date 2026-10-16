
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Reports;

// =====================================================
// Developer: NhaNTCE181298
// UC-F11-01, F11-02, F11-03, F11-04
// DTO: Bộ lọc và dữ liệu báo cáo vận hành.
// =====================================================

public class ReportPeriodQuery
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class RoomReportQuery : ReportPeriodQuery
{
    public int? RoomTypeId { get; set; }
}

public class ReservationReportQuery : ReportPeriodQuery
{
    public int? RoomTypeId { get; set; }
    public ReservationStatus? Status { get; set; }
}

public class RevenueReportQuery : ReportPeriodQuery
{
    public PaymentMethod? PaymentMethod { get; set; }
}

public class TaskReportQuery : ReportPeriodQuery
{
    public HotelTaskType? TaskType { get; set; }
    public HotelTaskStatus? Status { get; set; }
    public int? AssignedToAccountId { get; set; }

    // created hoặc completed
    public string DateField { get; set; } = "created";
}

// Dùng chung cho các nhóm thống kê.
public class ReportCountItem
{
    public string Key { get; set; } = "";
    public int Count { get; set; }
}

// =====================================================
// F11-01 - Room Report
// =====================================================

public class RoomReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int OutOfServiceRooms { get; set; }
    public int CurrentlyOccupiedRooms { get; set; }
    public int CurrentlyVacantRooms { get; set; }
    public int CurrentlyBlockedRooms { get; set; }

    // Thống kê phát sinh trong khoảng thời gian.
    public int RoomsWithStaysInPeriod { get; set; }
    public int StayCountInPeriod { get; set; }
    public int RoomsWithReservationsInPeriod { get; set; }
    public int RoomsBlockedInPeriod { get; set; }

    public List<ReportCountItem> ByHousekeepingStatus { get; set; } = new();
    public List<ReportCountItem> ByRoomType { get; set; } = new();
}

// =====================================================
// F11-02 - Reservation Report
// =====================================================

public class ReservationReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateField { get; set; } = "CheckInDate";
    public int TotalReservations { get; set; }
    public int TotalBookedNights { get; set; }
    public int TotalGuests { get; set; }

    public List<ReportCountItem> ByStatus { get; set; } = new();
    public List<ReportCountItem> ByRoomType { get; set; } = new();
}

// =====================================================
// F11-03 - Revenue Report
// =====================================================

public class RevenueMethodItem
{
    public string Method { get; set; } = "";
    public int PaymentCount { get; set; }
    public decimal Amount { get; set; }
}

public class RevenueReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateField { get; set; } = "PaidAt (UTC)";
    public int PaymentCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<RevenueMethodItem> ByMethod { get; set; } = new();
}

// =====================================================
// F11-04 - Task Report
// =====================================================

public class TaskReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateField { get; set; } = "CreatedAt";
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int UncompletedTasks { get; set; }
    public int AssignedTasks { get; set; }

    public List<ReportCountItem> ByTaskType { get; set; } = new();
    public List<ReportCountItem> ByStatus { get; set; } = new();
}
