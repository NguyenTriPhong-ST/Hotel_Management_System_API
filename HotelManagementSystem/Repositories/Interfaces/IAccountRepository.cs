using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Repositories.Interfaces;

/// <summary>
/// Interface định nghĩa các thao tác dữ liệu cho Entity Account.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Lấy tài khoản theo ID.
    /// </summary>
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tài khoản theo username đã chuẩn hóa (chữ hoa, không khoảng trắng thừa).
    /// </summary>
    Task<Account?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem username đã chuẩn hóa đã tồn tại chưa (có thể loại trừ ID hiện tại khi cập nhật).
    /// </summary>
    Task<bool> ExistsByNormalizedUsernameAsync(string normalizedUsername, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách tài khoản có phân trang, lọc và sắp xếp.
    /// </summary>
    Task<(IReadOnlyList<Account> Items, int TotalCount)> GetPagedListAsync(AccountFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm tài khoản mới.
    /// </summary>
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật tài khoản.
    /// </summary>
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu các thay đổi xuống CSDL.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
