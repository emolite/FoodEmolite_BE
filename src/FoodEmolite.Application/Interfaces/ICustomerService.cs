using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface ICustomerService
{
    /// <summary>Danh sách khách hàng (đã đăng ký + vãng lai) đã từng mua tại cửa hàng của agent đang đăng nhập.</summary>
    Task<BaseTableResponse<CustomerListItemDto>> GetAgentCustomersAsync(long currentUserId, BaseSearchRequest<CustomerSearchRequest> request);

    /// <summary>Admin — danh sách khách hàng trên toàn hệ thống, có thể lọc theo 1 cửa hàng qua SearchParams.StoreRefCode.</summary>
    Task<BaseTableResponse<CustomerListItemDto>> GetAdminCustomersAsync(BaseSearchRequest<CustomerSearchRequest> request);
}
