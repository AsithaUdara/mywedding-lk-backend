using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Vendors.Application.Helpers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors
{
    public class GetVendorsQueryHandler : IRequestHandler<GetVendorsQuery, IEnumerable<VendorDto>>
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IVendorSubscriptionRepository _subscriptionRepository;

        public GetVendorsQueryHandler(
            IVendorRepository vendorRepository,
            IVendorSubscriptionRepository subscriptionRepository)
        {
            _vendorRepository = vendorRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<IEnumerable<VendorDto>> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
        {
            var vendors = await _vendorRepository.GetAllAsync(cancellationToken);

            vendors = vendors.Where(v =>
                v.VerificationStatus == VerificationStatus.Verified &&
                VendorMediaHelper.ActiveServices(v).Any());

            if (!string.IsNullOrEmpty(request.Category))
            {
                vendors = vendors.Where(v =>
                {
                    var activeServices = VendorMediaHelper.ActiveServices(v).ToList();
                    if (activeServices.Count > 0)
                    {
                        return activeServices.Any(s =>
                            s.Category!.Name.Contains(request.Category, StringComparison.OrdinalIgnoreCase));
                    }

                    return (v.PrimaryCategory?.Name ?? "")
                        .Contains(request.Category, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (!string.IsNullOrEmpty(request.Location))
            {
                vendors = vendors.Where(v => (v.City ?? "").Contains(request.Location, StringComparison.OrdinalIgnoreCase));
            }

            var vendorDtos = vendors.Select(v =>
            {
                var activeServices = VendorMediaHelper.ActiveServices(v).ToList();

                if (activeServices.Count == 0)
                {
                    return new VendorDto(
                        v.UserId,
                        v.BusinessName,
                        v.BusinessDescription,
                        v.WebsiteUrl,
                        v.ContactPhone,
                        v.City ?? "N/A",
                        v.VerificationStatus.ToString(),
                        v.AverageRating,
                        v.Reviews.Count,
                        0,
                        v.PrimaryCategory?.Name ?? "Other",
                        VendorMediaHelper.ResolvePrimaryImageUrl(v, activeServices),
                        VendorMediaHelper.ResolveGalleryUrls(v, activeServices)
                    );
                }

                var minPrice = activeServices.Min(s => s.BasePrice);
                var primaryCategory = activeServices
                    .OrderBy(s => s.BasePrice)
                    .FirstOrDefault()?.Category?.Name ?? "Uncategorized";

                return new VendorDto(
                    v.UserId,
                    v.BusinessName,
                    v.BusinessDescription,
                    v.WebsiteUrl,
                    v.ContactPhone,
                    v.City ?? "N/A",
                    v.VerificationStatus.ToString(),
                    v.AverageRating,
                    v.Reviews.Count,
                    minPrice,
                    primaryCategory,
                    VendorMediaHelper.ResolvePrimaryImageUrl(v, activeServices),
                    VendorMediaHelper.ResolveGalleryUrls(v, activeServices)
                );
            }).ToList();

            var tiers = await _subscriptionRepository.GetActiveTiersByVendorIdsAsync(
                vendorDtos.Select(v => v.UserId),
                cancellationToken);

            return VendorRankingHelper.ApplySubscriptionRanking(vendorDtos, tiers);
        }
    }
}
