using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FoodEmolite.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<string>> RegisterAsync(RegisterRequest request)
    {
        var repoAccount =
            _unitOfWork.GetRepository<Account>();

        var existedEmail = await repoAccount.AnyAsync(x => x.Email == request.Email);

        if (existedEmail)
        {
            return BaseResponse<string>.Fail("Email already exists");
        }

        var existedUsername = await repoAccount
            .AnyAsync(x =>
                x.Username == request.Username);

        if (existedUsername)
        {
            return BaseResponse<string>.Fail("Username already exists");
        }

        var account = new Account
        {
            RefCode = Guid.NewGuid().ToString().ToUpper(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        await repoAccount.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Register successfully");
    }

    public async Task<BaseResponse<string>> CreateAgent(RegisterRequest request, string refCode)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();

        var existedEmail = await repoAccount.AnyAsync(x => x.Email == request.Email);

        if (existedEmail)
        {
            return BaseResponse<string>.Fail("Email already exists");
        }

        var existedUsername = await repoAccount
            .AnyAsync(x =>
                x.Username == request.Username);

        if (existedUsername)
        {
            return BaseResponse<string>.Fail("Username already exists");
        }

        var account = new Account
        {
            RefCode = refCode,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Agent"
        };

        await repoAccount.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Create account successfully");
    }
    public async Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoSession = _unitOfWork.GetRepository<UserSession>();

        var account = await repoAccount
            .FirstOrDefaultAsync(x =>
                x.Email == request.Input ||
                x.Username == request.Input);

        if (account is null)
        {
            return BaseResponse<LoginResponse>
                .Fail("Account not found");
        }

        var isValid = BCrypt.Net.BCrypt
            .Verify(
                request.Password,
                account.PasswordHash);

        if (!isValid)
        {
            return BaseResponse<LoginResponse>
                .Fail("Password incorrect");
        }

        var httpContext = _httpContextAccessor.HttpContext;

        var ipAddress = httpContext?
            .Request
            .Headers["X-Forwarded-For"]
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = httpContext?
                .Connection
                ?.RemoteIpAddress
                ?.ToString();
        }

        var userAgent = httpContext?
            .Request
            .Headers["User-Agent"]
            .ToString();

        var deviceName = userAgent;
        var now = DateTime.Now;
        var refreshToken = Guid.NewGuid().ToString();
        var existingSession = await repoSession
            .FirstOrDefaultAsync(x =>
                x.AccountId == account.Id &&
                x.IpAddress == ipAddress &&
                x.UserAgent == userAgent &&
                x.ExpiredAt > now &&
                !x.IsDeleted);

        if (existingSession is null)
        {
            var newSession = new UserSession
            {
                AccountId = account.Id,
                RefreshToken = refreshToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceName = deviceName,
                IsVerified = false,
                LastAccessAt = now,
                ExpiredAt = now.AddDays(30),
                IsActived = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = account.Id
            };

            await repoSession.AddAsync(newSession);
        }
        else
        {
            existingSession.LastAccessAt = now;
            existingSession.UpdatedAt = now;
            existingSession.UpdatedBy = account.Id;

            repoSession.Update(existingSession);
        }

        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<LoginResponse>
            .Success(new LoginResponse
            {
                Token = JwtHelper.GenerateToken(
                    account,
                    _configuration),

                ExpiredAt = JwtHelper
                    .GetExpiredTime(_configuration)
            });
    }
    public async Task<BaseResponse<bool>> CheckEmailAsync(string email)
    {
        var repoAccount =_unitOfWork.GetRepository<Account>();
        var existed = await repoAccount.AnyAsync(x => x.Email == email);

        return BaseResponse<bool>.Success(existed);
    }
    public async Task<BaseResponse<CurrentUserResponse>> VerifyAsync(long userId)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();

        var account = await repoAccount
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (account is null)
            return BaseResponse<CurrentUserResponse>
                .Fail("Account not found");

        return BaseResponse<CurrentUserResponse>
            .Success(new CurrentUserResponse
            {
                Id = account.Id,
                RefCode = account.RefCode,
                Username = account.Username,
                Email = account.Email,
                Role = account.Role
            });
    }
}