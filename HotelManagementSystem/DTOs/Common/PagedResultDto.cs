namespace HotelManagementSystem.DTOs.Common;

/// <summary>
/// DTO chứa kết quả phân trang chung cho hệ thống.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu phần tử trong danh sách.</typeparam>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedResultDto() { }

    public PagedResultDto(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
