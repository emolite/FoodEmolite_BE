using FoodEmolite.Application.DTOs.Realtime;

namespace FoodEmolite.Application.Interfaces;

public interface IRealtimeNotificationService
{
    Task NotifyNewOrderAsync(NewOrderNotificationDto notification);

    Task NotifyFoodQuantityChangedAsync(FoodQuantityChangedDto notification);

    Task NotifyPromotionStatusChangedAsync(PromotionStatusChangedDto notification);

    Task SendToStoreGroupAsync(string storeRefCode, string eventName, object payload);
}
