using AutoMapper;
using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.DTOs.Auth;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ xác thực tài khoản và quản lý phiên (F01).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthSessionRepository _sessionRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AuthService(
        IAccountRepository accountRepository,
        IAuthSessionRepository sessionRepository,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService,
        IMapper mapper)
    {
        _accountRepository = accountRepository;
        _sessionRepository = sessionRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    /// <summary>
    /// UC-F01-01 - Đăng nhập.
    /// Kiểm tra username, password và trạng thái tài khoản; tạo phiên và cấp JWT.
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var account = await _accountRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);

        if (account == null)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!account.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa hoặc khóa truy cập.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        // Tạo phiên đăng nhập mới trong AuthSessions
        var sessionId = Guid.NewGuid();
        var (token, expiresAt) = _tokenService.GenerateToken(account, sessionId);

        var session = new AuthSession
        {
            Id = sessionId,
            AccountId = account.Id,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            RevokedAt = null
        };

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        var accountDto = _mapper.Map<AccountDto>(account);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            SessionId = sessionId,
            Account = accountDto
        };
    }

    /// <summary>
    /// UC-F01-02 - Đăng xuất.
    /// Thu hồi phiên làm việc hiện tại, làm token mất hiệu lực.
    /// </summary>
    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var revoked = await _sessionRepository.RevokeSessionAsync(sessionId, cancellationToken);
        if (revoked)
        {
            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại từ ID.
    /// </summary>
    public async Task<AccountDto> GetCurrentAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản.");
        }

        return _mapper.Map<AccountDto>(account);
    }
}
