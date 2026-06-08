using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IOrderService
{
    Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, CreateOrderRequestDto request);

    Task<BaseTableResponse<OrderResponseDto>> GetMyOrdersAsync(long currentUserId, int page, int pageSize);

    Task<BaseResponse<OrderResponseDto>> GetDetailAsync(long id, long currentUserId);

    Task<BaseTableResponse<OrderResponseDto>> GetByStoreRefCodeAsync(string storeRefCode, int page, int pageSize);

    Task<BaseResponse<string>> UpdateStatusAsync(long id, long currentUserId, string refCode, UpdateOrderStatusRequestDto request);
}