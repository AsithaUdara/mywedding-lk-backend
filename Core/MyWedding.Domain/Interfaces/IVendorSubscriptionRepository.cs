using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;

namespace MyWedding.Domain.Interfaces
{
    public interface IVendorSubscriptionRepository
    {
        Task<IReadOnlyDictionary<string, SubscriptionPlanTier>> GetActiveTiersByVendorIdsAsync(
            IEnumerable<string> vendorIds,
            CancellationToken cancellationToken = default);

        Task CancelActiveSubscriptionsAsync(
            string vendorId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            VendorSubscription subscription,
            CancellationToken cancellationToken = default);
    }
}
