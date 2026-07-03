
using FoodEmolite.Application.DTOs.SePay;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        var orderCode = ExtractOrderCode(request.Content);

        var order = orderCode != null
            ? await repoOrder.FirstOrDefaultAsync(x => x.OrderCode.Replace("-", "") == orderCode)
            : null;

        var transaction = new PaymentTransaction
        {
            Gateway = "SEPAY",
            TransactionId = request.TransactionId,
            ReferenceCode = request.ReferenceCode,
            AccountNumber = request.AccountNumber,
            TransferType = request.TransferType,
            TransferAmount = request.Amount,
            Accumulated = request.Accumulated,
            Content = request.Content,
            Description = request.Description,
            TransactionDate = now,
            RawData = JsonSerializer.Serialize(request),
            IsProcessed = false,
            IsDeleted = false,
            OrderId = order?.Id,
            CreatedAt = now,
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

    private static string? ExtractOrderCode(string? content)
    {
        if (string.IsNullOrEmpty(content)) return null;

        var match = Regex.Match(content, @"ORDER(?<code>[A-Za-z0-9]+)\.CT");
        return match.Success ? match.Groups["code"].Value : null;
    }
}