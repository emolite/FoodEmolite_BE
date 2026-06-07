using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IStoreService
{
    Task<BaseResponse<string>> CreateAsync(
        long currentUserId,
        CreateStoreRequestDto request);

    Task<BaseResponse<string>> UpdateAsync(
        string refCode,
        long currentUserId,
        UpdateStoreRequestDto request);

    Task<BaseResponse<string>> DeleteAsync(
        string refCode,
        long currentUserId);

    Task<BaseTableResponse<StoreResponseDto>> GetAllAsync(
        int page,
        int pageSize);

    Task<BaseTableResponse<StoreResponseDto>> GetByOwnerRefCodeAsync(
        string ownerRefCode,
        int page,
        int pageSize);

    Task<BaseResponse<StoreResponseDto>> GetDetailAsync(string refCode);
}