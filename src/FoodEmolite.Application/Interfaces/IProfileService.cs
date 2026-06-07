using FoodEmolite.Application.DTOs.Profile;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IProfileService
{
    Task<BaseResponse<MyProfileResponseDto>> GetMyProfileAsync(
        long currentUserId);

    Task<BaseResponse<AccountProfileDto>> CreateAccountProfileAsync(
        long currentUserId,
        string currentUserRefCode,
        CreateAccountProfileRequestDto request);

    Task<BaseResponse<AccountProfileDto>> UpdateAccountProfileAsync(
        long currentUserId,
        string currentUserRefCode,
        UpdateAccountProfileRequestDto request);

    Task<BaseResponse<BankAccountDto>> CreateBankAccountAsync(
        long currentUserId,
        string currentUserRefCode,
        CreateBankAccountRequestDto request);

    Task<BaseResponse<BankAccountDto>> UpdateBankAccountAsync(
        long currentUserId,
        string currentUserRefCode,
        UpdateBankAccountRequestDto request);
}