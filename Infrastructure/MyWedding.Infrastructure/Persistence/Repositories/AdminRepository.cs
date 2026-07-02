using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default)
            => _context.Users.CountAsync(cancellationToken);

        public Task<int> GetTotalVendorsAsync(CancellationToken cancellationToken = default)
            => _context.Vendors.CountAsync(cancellationToken);

        public Task<int> GetTotalEventsAsync(CancellationToken cancellationToken = default)
            => _context.WeddingEvents.IgnoreQueryFilters().CountAsync(cancellationToken);

        public Task<int> GetTotalBookingsAsync(CancellationToken cancellationToken = default)
            => _context.VendorBookings.CountAsync(cancellationToken);

        public async Task<PlatformAnalyticsSnapshot> GetPlatformAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var thirtyDaysAgo = now.AddDays(-30);

            var currentMrr = await _context.PlannerSubscriptions
                .AsNoTracking()
                .Where(s => s.Status == SubscriptionStatus.Active)
                .SumAsync(s => s.MonthlyFee, cancellationToken);

            var lastMonthMrr = await _context.PlannerSubscriptions
                .AsNoTracking()
                .Where(s => s.Status == SubscriptionStatus.Active
                            && s.StartsAt < thisMonthStart
                            && (s.EndsAt == null || s.EndsAt >= lastMonthStart))
                .SumAsync(s => s.MonthlyFee, cancellationToken);

            var mrrDeltaPct = lastMonthMrr > 0
                ? Math.Round((currentMrr - lastMonthMrr) / lastMonthMrr * 100m, 1)
                : (currentMrr > 0 ? 100m : 0m);

            var tpv = await _context.BookingPaymentTransactions
                .AsNoTracking()
                .Where(t => t.Status == PaymentTransactionStatus.Paid)
                .SumAsync(t => t.Amount, cancellationToken);

            var tpvThisMonth = await _context.BookingPaymentTransactions
                .AsNoTracking()
                .Where(t => t.Status == PaymentTransactionStatus.Paid && t.PaidAt >= thisMonthStart)
                .SumAsync(t => t.Amount, cancellationToken);

            var tpvLastMonth = await _context.BookingPaymentTransactions
                .AsNoTracking()
                .Where(t => t.Status == PaymentTransactionStatus.Paid
                            && t.PaidAt >= lastMonthStart
                            && t.PaidAt < thisMonthStart)
                .SumAsync(t => t.Amount, cancellationToken);

            var tpvDeltaPct = tpvLastMonth > 0
                ? Math.Round((tpvThisMonth - tpvLastMonth) / tpvLastMonth * 100m, 1)
                : (tpvThisMonth > 0 ? 100m : 0m);

            var takeRateRevenue = await _context.CommissionSettlements
                .AsNoTracking()
                .SumAsync(c => c.CommissionAmount, cancellationToken);

            var activePlanners = await _context.WeddingPlanners
                .AsNoTracking()
                .CountAsync(p => p.IsActive && (
                    _context.PlannerSubscriptions.Any(s =>
                        s.PlannerId == p.UserId && s.Status == SubscriptionStatus.Active)
                    || _context.PlannerClientEvents.Any(e =>
                        e.PlannerId == p.UserId && e.UpdatedAt >= thirtyDaysAgo)),
                    cancellationToken);

            var activeCouples = await _context.PlannerClientEvents
                .AsNoTracking()
                .Where(e => e.UpdatedAt >= thirtyDaysAgo)
                .Select(e => e.ClientUserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var registeredVendors = await _context.Vendors
                .AsNoTracking()
                .CountAsync(v => v.VerificationStatus == VerificationStatus.Verified, cancellationToken);

            var usersWithEvents = await _context.WeddingEvents
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Select(e => e.CreatedById)
                .Distinct()
                .CountAsync(cancellationToken);

            var usersWithBookings = await _context.VendorBookings
                .AsNoTracking()
                .Select(b => b.BookedById)
                .Distinct()
                .CountAsync(cancellationToken);

            var eventsWithBookings = await _context.VendorBookings
                .AsNoTracking()
                .Select(b => b.EventId)
                .Distinct()
                .CountAsync(cancellationToken);

            var plannerGrowth = new List<MonthlyCountPoint>();
            for (var i = 5; i >= 0; i--)
            {
                var monthDate = thisMonthStart.AddMonths(-i);
                var endOfMonth = monthDate.AddMonths(1).AddTicks(-1);
                var count = await _context.WeddingPlanners
                    .AsNoTracking()
                    .CountAsync(p => p.IsActive && p.CreatedAt <= endOfMonth, cancellationToken);

                plannerGrowth.Add(new MonthlyCountPoint
                {
                    Month = monthDate.ToString("MMM"),
                    Count = count
                });
            }

            return new PlatformAnalyticsSnapshot
            {
                Mrr = currentMrr,
                MrrDeltaPct = mrrDeltaPct,
                Tpv = tpv,
                TpvDeltaPct = tpvDeltaPct,
                TakeRateRevenue = takeRateRevenue,
                ActivePlanners = activePlanners,
                ActiveCouples = activeCouples,
                RegisteredVendors = registeredVendors,
                TotalUsers = await GetTotalUsersAsync(cancellationToken),
                TotalEvents = await GetTotalEventsAsync(cancellationToken),
                TotalBookings = await GetTotalBookingsAsync(cancellationToken),
                UsersWithEvents = usersWithEvents,
                UsersWithBookings = usersWithBookings,
                EventsWithBookings = eventsWithBookings,
                PlannerGrowthByMonth = plannerGrowth
            };
        }
    }
}
