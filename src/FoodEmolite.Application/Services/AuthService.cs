using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.Extensions.Configuration;

namespace FoodEmolite.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
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
            RefCode = Guid
                .NewGuid()
                .ToString(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        await repoAccount.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Register successfully");
    }
    public async Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();

        var account = await repoAccount
            .FirstOrDefaultAsync(x =>
                x.Email == request.Input ||
                x.Username == request.Input);

        if (account is null)
        {
            return BaseResponse<LoginResponse>.Fail("Account not found");
        }

        var isValid = BCrypt.Net.BCrypt
            .Verify(
                request.Password,
                account.PasswordHash);

        if (!isValid)
        {
            return BaseResponse<LoginResponse>.Fail("Password incorrect");
        }

        return BaseResponse<LoginResponse>
            .Success(new LoginResponse
            {
                Token = JwtHelper
                    .GenerateToken(
                        account,
                        _configuration),

                ExpiredAt = JwtHelper
                    .GetExpiredTime(
                        _configuration)
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