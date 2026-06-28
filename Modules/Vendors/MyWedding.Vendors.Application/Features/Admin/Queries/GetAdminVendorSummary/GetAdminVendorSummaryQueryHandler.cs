using MediatR;
using MyWedding.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendorSummary;

public class GetAdminVendorSummaryQueryHandler
    : IRequestHandler<GetAdminVendorSummaryQuery, AdminVendorSummaryDto>
{
    private readonly IVendorRepository _vendorRepository;

    public GetAdminVendorSummaryQueryHandler(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public async Task<AdminVendorSummaryDto> Handle(
        GetAdminVendorSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _vendorRepository.GetAdminVendorSummaryAsync(cancellationToken);

        return new AdminVendorSummaryDto
        {
            Total = summary.Total,
            Verified = summary.Verified,
            Pending = summary.Pending,
            Rejected = summary.Rejected,
            LiveListings = summary.LiveListings,
            Categories = summary.Categories,
        };
    }
}
