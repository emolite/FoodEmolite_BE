
using FoodEmolite.Application.DTOs.SePay;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using System.Text.Json;

public class SePayWebhookService : ISePayWebhookService
{
    private readonly IUnitOfWork _unitOfWork;

    public SePayWebhookService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(SePayWebhookRequest request)
    {
        var repoTransaction = _unitOfWork.GetRepository<PaymentTransaction>();
        var repoOrder = _unitOfWork.GetRepository<Order>();

        var now = DateTime.Now;

        var existed = await repoTransaction.FirstOrDefaultAsync(x =>
            x.TransactionId == request.TransactionId);

        if (existed != null)
            return;

        var orderCode = request.Content?.Replace("ORDER_", "");

        var order = await repoOrder.FirstOrDefaultAsync(x =>
            x.OrderCode == orderCode);

        var transaction = new PaymentTransaction
        {
            Gateway = "SEPAY",
            TransactionId = request.TransactionId,
            ReferenceCode = orderCode,
            AccountNumber = request.AccountNumber,
            TransferAmount = request.Amount,
            Content = request.Content,
            Description = request.Description,
            TransactionDate = now,
            RawData = JsonSerializer.Serialize(request),
            IsProcessed = false,
            OrderId = order?.Id
        };

        await repoTransaction.AddAsync(transaction);

        if (order != null && order.PaymentStatus != "PAID" && order.TotalAmount == request.Amount)
        {
            order.PaymentStatus = "PAID";
            transaction.IsProcessed = true;
            transaction.ProcessedAt = now;
        }

        await _unitOfWork.SaveChangesAsync();
    }
}