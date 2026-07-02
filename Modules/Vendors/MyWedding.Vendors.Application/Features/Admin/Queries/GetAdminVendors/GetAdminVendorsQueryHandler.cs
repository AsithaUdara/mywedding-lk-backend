using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors;

public class GetAdminVendorsQueryHandler : IRequestHandler<GetAdminVendorsQuery, PagedResult<AdminVendorDto>>
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

    public async Task<PagedResult<AdminVendorDto>> Handle(
        GetAdminVendorsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _vendorRepository.GetAdminVendorsPagedAsync(
            request.Status,
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        var vendorList = page.Items.Select(v => new AdminVendorDto
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

        if (vendorList.Count > 0)
        {
            var tiers = await _subscriptionRepository.GetActiveTiersByVendorIdsAsync(
                vendorList.Select(v => v.UserId),
                cancellationToken);

            foreach (var vendor in vendorList)
            {
                vendor.SubscriptionTier = tiers.TryGetValue(vendor.UserId, out var tier)
                    ? tier.ToString()
                    : SubscriptionPlanTier.Free.ToString();
            }
        }

        return new PagedResult<AdminVendorDto>
        {
            Items = vendorList,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }
}
