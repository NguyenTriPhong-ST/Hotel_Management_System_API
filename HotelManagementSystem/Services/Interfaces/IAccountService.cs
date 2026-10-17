using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface định nghĩa nghiệp vụ quản lý tài khoản nhân viên (F02).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// UC-F02-01 - Tạo tài khoản nhân viên.
    /// Kiểm tra username duy nhất, role hợp lệ và băm mật khẩu.
    /// </summary>
    Task<AccountDto> CreateAccountAsync(CreateAccountRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F02-02 & UC-F02-07 - Xem danh sách, tìm kiếm và lọc tài khoản.
    /// Hỗ trợ phân trang, sắp xếp và nhiều điều kiện lọc.
    /// </summary>
    Task<PagedResultDto<AccountDto>> GetAccountsAsync(AccountFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F02-03 - Xem chi tiết tài khoản theo ID.
    /// </summary>
    Task<AccountDto> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F02-04 - Cập nhật thông tin tài khoản.
    /// Validate dữ liệu cập nhật và chống trùng username.
    /// </summary>
    Task<AccountDto> UpdateAccountAsync(int id, UpdateAccountRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F02-05 - Thay đổi role tài khoản.
    /// Role được gán phải hợp lệ và thu hồi toàn bộ phiên của tài khoản để đồng bộ quyền mới.
    /// </summary>
    Task<AccountDto> ChangeRoleAsync(int id, AccountRole newRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// UC-F02-06 - Kích hoạt hoặc vô hiệu hóa tài khoản.
    /// Cập nhật trạng thái và thu hồi toàn bộ phiên truy cập nếu tài khoản bị khóa.
    /// </summary>
    Task<AccountDto> ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
