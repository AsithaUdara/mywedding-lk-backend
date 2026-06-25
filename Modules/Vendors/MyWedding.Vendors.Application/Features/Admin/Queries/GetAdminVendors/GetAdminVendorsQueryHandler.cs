using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors
{
    public class GetAdminVendorsQueryHandler : IRequestHandler<GetAdminVendorsQuery, IEnumerable<AdminVendorDto>>
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IVendorSubscriptionRepository _subscriptionRepository;

        public GetAdminVendorsQueryHandler(
            IVendorRepository vendorRepository,
            IVendorSubscriptionRepository subscriptionRepository)
        {
            _vendorRepository = vendorRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<IEnumerable<AdminVendorDto>> Handle(
            GetAdminVendorsQuery request,
            CancellationToken cancellationToken)
        {
            var vendors = await _vendorRepository.GetAdminVendorsAsync(request.Status, cancellationToken);

            var vendorList = vendors.Select(v => new AdminVendorDto
            {
                UserId = v.UserId,
                BusinessName = v.BusinessName,
                BusinessDescription = v.BusinessDescription,
                City = v.City,
                CategoryName = v.PrimaryCategory?.Name,
                OwnerEmail = v.User?.Email,
                OwnerName = v.User != null ? $"{v.User.FirstName} {v.User.LastName}" : null,
                VerificationStatus = v.VerificationStatus.ToString(),
                ActiveServiceCount = v.Services.Count(s => s.IsActive),
                AverageRating = v.AverageRating,
                RegisteredAt = v.User?.CreatedAt ?? DateTime.UtcNow,
            }).ToList();

            if (vendorList.Count == 0)
            {
                return vendorList;
            }

            var tiers = await _subscriptionRepository.GetActiveTiersByVendorIdsAsync(
                vendorList.Select(v => v.UserId),
                cancellationToken);

            foreach (var vendor in vendorList)
            {
                vendor.SubscriptionTier = tiers.TryGetValue(vendor.UserId, out var tier)
                    ? tier.ToString()
                    : SubscriptionPlanTier.Free.ToString();
            }

            return vendorList;
        }
    }
}
