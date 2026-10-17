namespace HotelManagementSystem.DTOs.Common;

/// <summary>
/// Mô hình phân trang dùng chung cho toàn bộ hệ thống.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của danh sách phần tử.</typeparam>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
