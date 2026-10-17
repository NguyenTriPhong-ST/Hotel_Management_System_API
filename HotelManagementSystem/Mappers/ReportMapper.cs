
using HotelManagementSystem.DTOs.Reports;

namespace HotelManagementSystem.Mappers;

// =====================================================
// Developer: NhaNTCE181298
// Module: F11 - Operational Reporting
// Layer: Mapper
//
// UC-F11-01, 02, 04
// Chuyển nhóm thống kê thành ReportCountItem.
// =====================================================

public static class ReportMapper
{
    // NhaNT | F11-01, 02, 04
    // Nhóm và đếm dữ liệu đã tải từ Database.
    public static List<ReportCountItem> ToCountItems(
        IEnumerable<string> keys)
    {
        return keys
            .GroupBy(key => key)
            .Select(group => new ReportCountItem
            {
                Key = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Key)
            .ToList();
    }

    // NhaNT | F11-04
    // Chuyển số liệu đã GROUP BY từ SQL thành DTO.
    public static List<ReportCountItem> ToCountItems(
        IEnumerable<(string Key, int Count)> groups)
    {
        return groups
            .GroupBy(item => item.Key)
            .Select(group => new ReportCountItem
            {
                Key = group.Key,
                Count = group.Sum(item => item.Count)
            })
            .OrderBy(item => item.Key)
            .ToList();
    }
}
