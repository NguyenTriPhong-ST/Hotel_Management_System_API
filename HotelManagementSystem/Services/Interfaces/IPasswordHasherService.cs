namespace HotelManagementSystem.Services.Interfaces;

/// <summary>
/// Interface định nghĩa dịch vụ băm và kiểm tra mật khẩu.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Băm mật khẩu dạng plaintext thành chuỗi hash bảo mật.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Kiểm tra mật khẩu người dùng nhập có khớp với hash đã lưu hay không.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
