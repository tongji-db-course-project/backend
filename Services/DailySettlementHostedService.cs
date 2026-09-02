using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 单门店每日营业结转任务。服务器本地时间自然日结束后生成前一日结转，空营业日也会生成。
/// </summary>
public sealed class DailySettlementHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySettlementHostedService> _logger;

    public DailySettlementHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailySettlementHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 启动时先补齐最近一个已经闭店的营业日；已存在时服务会直接返回。
                await GenerateAsync(DateTime.Now.Date.AddDays(-1), stoppingToken);
                var now = DateTime.Now;
                await Task.Delay(now.Date.AddDays(1) - now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成每日营业结转失败");
                // 异常后一分钟重新检查最近一个已闭店营业日。
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task GenerateAsync(DateTime businessDate, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
        try
        {
            var result = await service.GenerateDailySettlementAsync(businessDate);
            _logger.LogInformation("营业日 {BusinessDate:yyyy-MM-dd} 日结已生成，销售实收 {TotalSales}，退款 {RefundAmount}，净销售额 {NetSales}",
                businessDate, result.totalSales, result.refundAmount, result.netSales);
        }
        catch (DbUpdateException ex)
        {
            // 多实例同时执行时由结转日期唯一约束兜底；另一实例成功即视为本次已完成。
            _logger.LogWarning(ex, "营业日 {BusinessDate:yyyy-MM-dd} 日结可能已由其他实例生成", businessDate);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
