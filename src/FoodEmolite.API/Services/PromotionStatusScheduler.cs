using FoodEmolite.Application.Interfaces;

namespace FoodEmolite.API.Services;

/// <summary>
/// Quét định kỳ để tự chuyển trạng thái Promotion (SCHEDULED/ACTIVE/ENDED) theo lịch đã cấu hình,
/// và bắn realtime cho FE ngay cả khi không ai đang mở trang khuyến mãi.
/// </summary>
public class PromotionStatusScheduler : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PromotionStatusScheduler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var promotionService = scope.ServiceProvider
                    .GetRequiredService<IPromotionService>();

                await promotionService.RecomputeStatusesAsync();
            }
            catch
            {
                // best-effort: bỏ qua lỗi 1 lượt quét, sẽ thử lại ở lượt kế tiếp
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
