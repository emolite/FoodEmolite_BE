using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IStoreFoodService
{
    Task<BaseResponse<string>> CreateAsync(string refCode, CreateStoreFoodRequestDto request);

    Task<BaseResponse<string>> UpdateAsync(string refCode, long id, UpdateStoreFoodRequestDto request);

    Task<BaseResponse<string>> DeleteAsync(long id);

    Task<BaseTableResponse<StoreFoodResponseDto>> GetAllAsync(
        int page,
        int pageSize);

    Task<BaseTableResponse<StoreFoodResponseDto>> GetByStoreRefCodeAsync(BaseSearchRequest<GetStoreFoodsRequest> request);

    Task<BaseResponse<StoreFoodResponseDto>> GetDetailAsync(long id);
}