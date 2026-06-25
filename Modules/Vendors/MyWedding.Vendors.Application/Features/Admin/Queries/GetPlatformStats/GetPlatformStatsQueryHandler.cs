using MediatR;
using MyWedding.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformStats
{
    public class GetPlatformStatsQueryHandler : IRequestHandler<GetPlatformStatsQuery, PlatformStatsDto>
    {
        private readonly IAdminRepository _adminRepository;

        public GetPlatformStatsQueryHandler(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public async Task<PlatformStatsDto> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
        {
            var totalUsers    = await _adminRepository.GetTotalUsersAsync(cancellationToken);
            var totalVendors  = await _adminRepository.GetTotalVendorsAsync(cancellationToken);
            var totalEvents   = await _adminRepository.GetTotalEventsAsync(cancellationToken);
            var totalBookings = await _adminRepository.GetTotalBookingsAsync(cancellationToken);

            return new PlatformStatsDto
            {
                TotalUsers    = totalUsers,
                TotalVendors  = totalVendors,
                TotalEvents   = totalEvents,
                TotalBookings = totalBookings
            };
        }
    }
}
