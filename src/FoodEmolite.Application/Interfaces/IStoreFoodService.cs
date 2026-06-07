using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IStoreFoodService
{
    Task<BaseResponse<string>> CreateAsync(CreateStoreFoodRequestDto request);

    Task<BaseResponse<string>> UpdateAsync(
        string refCode,
        UpdateStoreFoodRequestDto request);

    Task<BaseResponse<string>> DeleteAsync(string refCode);

    Task<BaseTableResponse<StoreFoodResponseDto>> GetAllAsync(
        int page,
        int pageSize);

    Task<BaseTableResponse<StoreFoodResponseDto>> GetByStoreRefCodeAsync(
        string storeRefCode,
        int page,
        int pageSize);

    Task<BaseResponse<StoreFoodResponseDto>> GetDetailAsync(string refCode);
}