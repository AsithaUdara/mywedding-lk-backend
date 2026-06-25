using Microsoft.Extensions.DependencyInjection;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.API.IntegrationTests;

public static class IntegrationTestSeed
{
    public const string ActivePlannerUserId = "integration-planner-active";
    public const string ExpiredPlannerUserId = "integration-planner-expired";

    public static async Task SeedActivePlannerAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedPlannerAsync(db, ActivePlannerUserId, SubscriptionStatus.Active);
    }

    public static async Task SeedExpiredPlannerAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedPlannerAsync(db, ExpiredPlannerUserId, SubscriptionStatus.Expired);
    }

    private static async Task SeedPlannerAsync(
        ApplicationDbContext db,
        string userId,
        SubscriptionStatus subscriptionStatus)
    {
        if (await db.WeddingPlanners.FindAsync(userId) is not null)
            return;

        var now = DateTime.UtcNow;

        db.Users.Add(new User
        {
            Id = userId,
            Email = $"{userId}@test.local",
            FirstName = "Integration",
            LastName = "Planner",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.WeddingPlanners.Add(new WeddingPlanner
        {
            UserId = userId,
            BusinessName = "Integration Test Agency",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.PlannerSubscriptions.Add(new PlannerSubscription
        {
            Id = Guid.NewGuid(),
            PlannerId = userId,
            Tier = SubscriptionPlanTier.PlannerPro,
            Status = subscriptionStatus,
            MonthlyFee = 0,
            MaxConcurrentEvents = 10,
            StartsAt = now.AddMonths(-1),
            EndsAt = subscriptionStatus == SubscriptionStatus.Expired ? now.AddDays(-1) : null,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
    }
}
