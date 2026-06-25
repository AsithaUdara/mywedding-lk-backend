using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.API.Workers;

/// <summary>
/// Marks planner subscriptions as expired when their end date has passed.
/// </summary>
public class SubscriptionExpiryWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryWorker> _logger;

    public SubscriptionExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunExpiryPassAsync(stoppingToken);

        using var timer = new PeriodicTimer(RunInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunExpiryPassAsync(stoppingToken);
        }
    }

    private async Task RunExpiryPassAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var due = await db.PlannerSubscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.EndsAt != null &&
                    s.EndsAt < now)
                .ToListAsync(cancellationToken);

            if (due.Count == 0)
                return;

            foreach (var subscription in due)
                subscription.Status = SubscriptionStatus.Expired;

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Expired {Count} planner subscription(s) with EndsAt before {UtcNow:O}",
                due.Count,
                now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Planner subscription expiry worker failed.");
        }
    }
}
