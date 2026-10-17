using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai băm mật khẩu bằng thuật toán BCrypt bảo mật cao.
/// </summary>
public class BcryptPasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
