using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IOrderService
{
    Task<BaseResponse<CreateOrderResponseDto>> CreateAsync(long currentUserId, string refCode, CreateOrderRequestDto request);
    Task<BaseResponse<CreateOrderResponseDto>> CreateGuestAsync(CreateGuestOrderRequestDto request);
    Task<BaseTableResponse<OrderResponseDto>> GetMyOrdersAsync(long currentUserId, int page, int pageSize);

    Task<BaseResponse<OrderResponseDto>> GetDetailAsync(long id, long currentUserId);

    Task<BaseTableResponse<OrderResponseDto>> GetByStoreRefCodeAsync(BaseSearchRequest<OrderSearchRequest> request);

    Task<BaseResponse<string>> UpdateStatusAsync(long id, long currentUserId, string refCode, UpdateOrderStatusRequestDto request);

    Task<BaseResponse<string>> UpdatePaymentStatusAsync(long id, long currentUserId, string refCode, UpdatePaymentStatusRequestDto request);

    Task<BaseResponse<string>> CancelAsync(long id, long currentUserId, string refCode);

    Task<BaseResponse<byte[]>> PrintOrdersAsync(long currentUserId, PrintOrdersRequestDto request);

    Task<BaseResponse<string>> GetPaymentStatusAsync(string orderCode);

    Task<BaseResponse<string?>> CheckPendingOrderAsync(string deviceId);
}