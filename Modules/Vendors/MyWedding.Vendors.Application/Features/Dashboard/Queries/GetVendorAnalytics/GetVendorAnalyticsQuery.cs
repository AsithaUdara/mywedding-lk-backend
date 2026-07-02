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
        // Services
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }

        // Bookings
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }

        // Revenue
        /// <summary>Sum of FinalAmount where Status == Completed.</summary>
        public decimal TotalRevenue { get; set; }
        /// <summary>Sum of FinalAmount where Status == Confirmed.</summary>
        public decimal PendingRevenue { get; set; }

        // Ratings
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Inquiries
        public int TotalInquiries { get; set; }
        public int UnreadInquiries { get; set; }

        // Trend chart
        public List<MonthlyEarningsDto> MonthlyEarnings { get; set; } = new();
    }

    public class MonthlyEarningsDto
    {
        public required string Month { get; set; }
        public decimal Amount { get; set; }
    }
}
