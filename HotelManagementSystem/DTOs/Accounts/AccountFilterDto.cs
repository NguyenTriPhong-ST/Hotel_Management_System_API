using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.DTOs.Accounts;

/// <summary>
/// DTO chứa các tiêu chí tìm kiếm, lọc và phân trang tài khoản (Admin).
/// </summary>
public class AccountFilterDto
{
    /// <summary>
    /// Từ khóa tìm kiếm theo tên đăng nhập hoặc họ tên nhân viên.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo vai trò: 1 = Admin, 2 = Receptionist, 3 = StaffRoom.
    /// </summary>
    public AccountRole? Role { get; set; }

    /// <summary>
    /// Lọc theo trạng thái hoạt động: true = Hoạt động, false = Bị khóa.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Số trang hiện tại (bắt đầu từ 1). Mặc định là 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên một trang. Mặc định là 10.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Tiêu chí sắp xếp: "username" hoặc "createdat" (ngày tạo). Mặc định là "username".
    /// </summary>
    public string? SortBy { get; set; } = "username";

    /// <summary>
    /// Hướng sắp xếp: true = giảm dần (DESC), false = tăng dần (ASC).
    /// </summary>
    public bool SortDescending { get; set; } = false;
}
