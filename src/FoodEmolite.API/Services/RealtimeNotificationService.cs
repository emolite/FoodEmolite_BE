using FoodEmolite.API.Hubs;
using FoodEmolite.Application.DTOs.Realtime;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FoodEmolite.API.Services;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public RealtimeNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyNewOrderAsync(NewOrderNotificationDto notification)
    {
        return SendToStoreGroupAsync(notification.StoreRefCode, "NewOrder", notification);
    }

    public async Task NotifyFoodQuantityChangedAsync(FoodQuantityChangedDto notification)
    {
        await SendToStoreGroupAsync(notification.StoreRefCode, "FoodQuantityChanged", notification);
        await SendToPublicStoreGroupAsync(notification.StoreRefCode, "FoodQuantityChanged", notification);
    }

    public async Task NotifyPromotionStatusChangedAsync(PromotionStatusChangedDto notification)
    {
        await SendToStoreGroupAsync(notification.StoreRefCode, "PromotionStatusChanged", notification);
        await SendToPublicStoreGroupAsync(notification.StoreRefCode, "PromotionStatusChanged", notification);
    }

    public Task SendToStoreGroupAsync(string storeRefCode, string eventName, object payload)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return Task.CompletedTask;

        return _hubContext.Clients
            .Group(NotificationHub.StoreGroupName(storeRefCode))
            .SendAsync(eventName, payload);
    }

    private Task SendToPublicStoreGroupAsync(string storeRefCode, string eventName, object payload)
    {
        if (string.IsNullOrWhiteSpace(storeRefCode))
            return Task.CompletedTask;

        return _hubContext.Clients
            .Group(NotificationHub.PublicStoreGroupName(storeRefCode))
            .SendAsync(eventName, payload);
    }
}
