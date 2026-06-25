using MediatR;
using MyWedding.Domain.ReadModels;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformAnalytics
{
    public class GetPlatformAnalyticsQuery : IRequest<PlatformAnalyticsSnapshot>
    {
    }
}
