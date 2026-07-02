using FoodEmolite.Application.DTOs.SePay;

namespace FoodEmolite.Application.ExternalService.Interfaces
{
    public interface ISePayWebhookService
    {
        Task HandleAsync(SePayWebhookRequest request);
    }
}
