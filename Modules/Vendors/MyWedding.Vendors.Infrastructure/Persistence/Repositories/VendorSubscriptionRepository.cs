using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.Vendors.Infrastructure.Persistence.Repositories
{
    public class VendorSubscriptionRepository : IVendorSubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorSubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyDictionary<string, SubscriptionPlanTier>> GetActiveTiersByVendorIdsAsync(
            IEnumerable<string> vendorIds,
            CancellationToken cancellationToken = default)
        {
            var ids = vendorIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<string, SubscriptionPlanTier>();
            }

            var subscriptions = await _context.VendorSubscriptions
                .AsNoTracking()
                .Where(s => ids.Contains(s.VendorId) && s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return subscriptions
                .GroupBy(s => s.VendorId)
                .ToDictionary(g => g.Key, g => g.First().Tier);
        }

        public async Task CancelActiveSubscriptionsAsync(
            string vendorId,
            CancellationToken cancellationToken = default)
        {
            var currentActive = await _context.VendorSubscriptions
                .Where(s => s.VendorId == vendorId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var existing in currentActive)
            {
                existing.Status = SubscriptionStatus.Cancelled;
                existing.EndsAt = DateTime.UtcNow;
            }
        }

        public async Task AddAsync(
            VendorSubscription subscription,
            CancellationToken cancellationToken = default)
        {
            await _context.VendorSubscriptions.AddAsync(subscription, cancellationToken);
        }
    }
}
