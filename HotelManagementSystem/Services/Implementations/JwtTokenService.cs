using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ tạo token JWT với các Claim cần thiết cho phân quyền và kiểm tra phiên.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(Account account, Guid sessionId)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "HotelManagementSystem_SuperSecretKey_For_JWT_Authentication_2026_PRN232";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "HotelManagementSystem";
        var audience = _configuration["JwtSettings:Audience"] ?? "HotelManagementClient";
        var expiryMinutesStr = _configuration["JwtSettings:ExpiryMinutes"];
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var minutes) ? minutes : 1440; // Mặc định 24 giờ

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, account.Role.ToString()),
            new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
            new Claim("FullName", account.FullName),
            new Claim("SessionId", sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return (tokenString, expiresAt);
    }
}
