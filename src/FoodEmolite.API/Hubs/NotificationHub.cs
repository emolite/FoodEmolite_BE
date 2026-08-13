using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodEmolite.API.Hubs;

public class NotificationHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationHub(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public static string StoreGroupName(string storeRefCode) => $"store:{storeRefCode}";

    public static string PublicStoreGroupName(string storeRefCode) => $"store-public:{storeRefCode}";

    /// <summary>
    /// Group riêng cho đại lý (chủ store) — nhận cả sự kiện đơn hàng mới lẫn giá/tồn kho.
    /// Yêu cầu JWT hợp lệ và phải là chủ sở hữu store.
    /// </summary>
    [Authorize]
    public async Task JoinStoreGroup(string storeRefCode)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return;

        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdValue) || !long.TryParse(userIdValue, out var userId))
            return;

        var repoStore = _unitOfWork.GetRepository<Store>();

        var isOwner = await repoStore
            .Query()
            .AnyAsync(x => x.RefCode == storeRefCode && x.OwnerAccountId == userId && !x.IsDeleted);

        if (!isOwner)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, StoreGroupName(storeRefCode));
    }

    [Authorize]
    public async Task LeaveStoreGroup(string storeRefCode)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, StoreGroupName(storeRefCode));
    }

    /// <summary>
    /// Group công khai cho khách hàng (kể cả khách vãng lai chưa đăng nhập) — chỉ nhận sự kiện
    /// giá/tồn kho/khuyến mãi (KHÔNG có thông báo đơn hàng mới, tránh lộ thông tin đơn của người khác).
    /// </summary>
    public async Task JoinPublicStoreGroup(string storeRefCode)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, PublicStoreGroupName(storeRefCode));
    }

    public async Task LeavePublicStoreGroup(string storeRefCode)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, PublicStoreGroupName(storeRefCode));
    }
}
