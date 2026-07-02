using MyWedding.Domain.Enums;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors;

namespace MyWedding.Vendors.Application.Helpers
{
    public static class VendorRankingHelper
    {
        private static readonly IReadOnlyDictionary<SubscriptionPlanTier, int> TierWeight =
            new Dictionary<SubscriptionPlanTier, int>
            {
                [SubscriptionPlanTier.Sponsored] = 3,
                [SubscriptionPlanTier.Featured] = 2,
                [SubscriptionPlanTier.Free] = 1,
                [SubscriptionPlanTier.PlannerPro] = 1
            };

        public static SubscriptionPlanTier ResolveTier(
            IReadOnlyDictionary<string, SubscriptionPlanTier> tiersByVendor,
            string vendorId)
        {
            return tiersByVendor.TryGetValue(vendorId, out var tier)
                ? tier
                : SubscriptionPlanTier.Free;
        }

        public static IEnumerable<VendorDto> ApplySubscriptionRanking(
            IEnumerable<VendorDto> vendors,
            IReadOnlyDictionary<string, SubscriptionPlanTier> tiersByVendor)
        {
            return vendors
                .Select(v =>
                {
                    var tier = ResolveTier(tiersByVendor, v.UserId);
                    return v with
                    {
                        PremiumTier = tier.ToString(),
                        IsSponsored = tier == SubscriptionPlanTier.Sponsored,
                        IsFeatured = tier == SubscriptionPlanTier.Featured
                    };
                })
                .OrderByDescending(v => TierWeight.TryGetValue(
                    Enum.Parse<SubscriptionPlanTier>(v.PremiumTier), out var weight) ? weight : 1)
                .ThenByDescending(v => v.AverageRating);
        }
    }
}
