using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformAnalytics
{
    public class GetPlatformAnalyticsQueryHandler : IRequestHandler<GetPlatformAnalyticsQuery, PlatformAnalyticsSnapshot>
    {
        private readonly IAdminRepository _adminRepository;

        public GetPlatformAnalyticsQueryHandler(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public Task<PlatformAnalyticsSnapshot> Handle(
            GetPlatformAnalyticsQuery request,
            CancellationToken cancellationToken)
        {
            return _adminRepository.GetPlatformAnalyticsAsync(cancellationToken);
        }
    }
}
