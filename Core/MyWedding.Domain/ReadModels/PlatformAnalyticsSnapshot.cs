using System.Collections.Generic;

namespace MyWedding.Domain.ReadModels
{
    public class PlatformAnalyticsSnapshot
    {
        public decimal Mrr { get; init; }
        public decimal MrrDeltaPct { get; init; }
        public decimal Tpv { get; init; }
        public decimal TpvDeltaPct { get; init; }
        public decimal TakeRateRevenue { get; init; }
        public int ActivePlanners { get; init; }
        public int ActiveCouples { get; init; }
        public int RegisteredVendors { get; init; }
        public int TotalUsers { get; init; }
        public int TotalEvents { get; init; }
        public int TotalBookings { get; init; }
        /// <summary>Distinct users who created at least one wedding event.</summary>
        public int UsersWithEvents { get; init; }
        /// <summary>Distinct users who made at least one vendor booking.</summary>
        public int UsersWithBookings { get; init; }
        /// <summary>Distinct events with at least one vendor booking.</summary>
        public int EventsWithBookings { get; init; }
        public IReadOnlyList<MonthlyCountPoint> PlannerGrowthByMonth { get; init; } = [];
    }

    public class MonthlyCountPoint
    {
        public required string Month { get; init; }
        public int Count { get; init; }
    }

    public class WeeklyViewCountPoint
    {
        public required string Week { get; init; }
        public int Views { get; init; }
    }

    public class WinRateSummary
    {
        public int Won { get; init; }
        public int Pending { get; init; }
        public int Lost { get; init; }
    }
}
