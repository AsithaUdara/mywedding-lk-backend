using MediatR;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPlatformStats
{
    public class GetPlatformStatsQuery : IRequest<PlatformStatsDto> { }

    public class PlatformStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalVendors { get; set; }
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
    }
}
