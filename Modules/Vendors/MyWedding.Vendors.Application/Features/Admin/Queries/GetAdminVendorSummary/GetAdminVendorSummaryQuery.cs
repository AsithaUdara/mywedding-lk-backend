using MediatR;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendorSummary;

public class GetAdminVendorSummaryQuery : IRequest<AdminVendorSummaryDto>
{
}

public class AdminVendorSummaryDto
{
    public int Total { get; set; }
    public int Verified { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public int LiveListings { get; set; }
    public int Categories { get; set; }
}
