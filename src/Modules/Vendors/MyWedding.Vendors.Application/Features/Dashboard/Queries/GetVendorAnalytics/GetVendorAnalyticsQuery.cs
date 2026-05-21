using MediatR;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Dashboard.Queries.GetVendorAnalytics
{
    public class GetVendorAnalyticsQuery : IRequest<VendorAnalyticsDto>
    {
        public required string VendorId { get; set; }
    }

    public class VendorAnalyticsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalInquiries { get; set; }
        public int UnreadInquiries { get; set; }
        public List<MonthlyEarningsDto> MonthlyEarnings { get; set; } = new();
    }

    public class MonthlyEarningsDto
    {
        public required string Month { get; set; }
        public decimal Amount { get; set; }
    }
}
