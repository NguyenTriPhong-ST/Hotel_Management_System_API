using AutoMapper;
using HotelManagementSystem.DTOs.Accounts;
using HotelManagementSystem.DTOs.Common;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Interfaces;

namespace HotelManagementSystem.Services.Implementations;

/// <summary>
/// Triển khai dịch vụ nghiệp vụ quản lý tài khoản nhân viên (F02).
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthSessionRepository _sessionRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IMapper _mapper;

    public AccountService(
        IAccountRepository accountRepository,
        IAuthSessionRepository sessionRepository,
        IPasswordHasherService passwordHasher,
        IMapper mapper)
    {
        _accountRepository = accountRepository;
        _sessionRepository = sessionRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    /// <summary>
    /// UC-F02-01 - Tạo tài khoản nhân viên.
    /// Kiểm tra username duy nhất, role hợp lệ và băm mật khẩu.
    /// </summary>
    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra vai trò hợp lệ thuộc các vai trò của hệ thống (Admin, Receptionist, StaffRoom)
        if (!Enum.IsDefined(typeof(AccountRole), request.Role))
        {
            throw new ArgumentException("Vai trò được chỉ định không hợp lệ trong hệ thống.");
        }

        // 2. Chuẩn hóa và kiểm tra username duy nhất
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var exists = await _accountRepository.ExistsByNormalizedUsernameAsync(normalizedUsername, null, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Tên đăng nhập '{request.Username.Trim()}' đã được sử dụng.");
        }

        // 3. Khởi tạo đối tượng Account và băm mật khẩu
        var account = new Account
        {
            Username = request.Username.Trim(),
            NormalizedUsername = normalizedUsername,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Role = request.Role,
            IsActive = true
        };

        await _accountRepository.AddAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AccountDto>(account);
    }

    /// <summary>
    /// UC-F02-02 & UC-F02-07 - Xem danh sách, tìm kiếm và lọc tài khoản.
    /// Phân trang, sắp xếp và kết hợp nhiều điều kiện lọc.
    /// </summary>
    public async Task<PagedResultDto<AccountDto>> GetAccountsAsync(AccountFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _accountRepository.GetPagedListAsync(filter, cancellationToken);
        var dtos = _mapper.Map<List<AccountDto>>(items);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        return new PagedResultDto<AccountDto>(dtos, totalCount, page, pageSize);
    }

    /// <summary>
    /// UC-F02-03 - Xem chi tiết tài khoản theo ID.
    /// </summary>
    public async Task<AccountDto> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã ID = {id}.");
        }

        return _mapper.Map<AccountDto>(account);
    }

    /// <summary>
    /// UC-F02-04 - Cập nhật thông tin tài khoản.
    /// Validate dữ liệu cập nhật và username trùng.
    /// </summary>
    public async Task<AccountDto> UpdateAccountAsync(int id, UpdateAccountRequestDto request, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã ID = {id}.");
        }

        // Nếu có thay đổi username thì kiểm tra trùng với tài khoản khác
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var newNormalized = request.Username.Trim().ToUpperInvariant();
            if (newNormalized != account.NormalizedUsername)
            {
                var isDuplicated = await _accountRepository.ExistsByNormalizedUsernameAsync(newNormalized, id, cancellationToken);
                if (isDuplicated)
                {
                    throw new InvalidOperationException($"Tên đăng nhập '{request.Username.Trim()}' đã được tài khoản khác sử dụng.");
                }

                account.Username = request.Username.Trim();
                account.NormalizedUsername = newNormalized;
            }
        }

        account.FullName = request.FullName.Trim();
        account.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        account.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AccountDto>(account);
    }

    /// <summary>
    /// UC-F02-05 - Thay đổi role tài khoản.
    /// Role được gán phải thuộc các role của hệ thống; thu hồi mọi phiên của tài khoản để đồng bộ quyền mới.
    /// </summary>
    public async Task<AccountDto> ChangeRoleAsync(int id, AccountRole newRole, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(AccountRole), newRole))
        {
            throw new ArgumentException("Vai trò mới không hợp lệ trong hệ thống.");
        }

        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã ID = {id}.");
        }

        account.Role = newRole;
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        // Thu hồi toàn bộ phiên làm việc để người dùng bắt buộc đăng nhập lại nhận token mới với vai trò mới
        await _sessionRepository.RevokeAllSessionsByAccountIdAsync(id, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AccountDto>(account);
    }

    /// <summary>
    /// UC-F02-06 - Kích hoạt hoặc vô hiệu hóa tài khoản.
    /// Cập nhật trạng thái; xử lý quyền truy cập của tài khoản bị khóa.
    /// </summary>
    public async Task<AccountDto> ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã ID = {id}.");
        }

        account.IsActive = isActive;
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        // Nếu vô hiệu hóa tài khoản (khóa), thu hồi toàn bộ phiên làm việc của tài khoản ngay lập tức
        if (!isActive)
        {
            await _sessionRepository.RevokeAllSessionsByAccountIdAsync(id, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<AccountDto>(account);
    }
}
