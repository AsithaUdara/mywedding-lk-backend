using MediatR;
using MyWedding.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Dashboard.Queries.GetVendorAnalytics
{
    public class GetVendorAnalyticsQueryHandler : IRequestHandler<GetVendorAnalyticsQuery, VendorAnalyticsDto>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IVendorInquiryRepository _inquiryRepository;

        public GetVendorAnalyticsQueryHandler(
            IVendorBookingRepository bookingRepository,
            IVendorInquiryRepository inquiryRepository)
        {
            _bookingRepository = bookingRepository;
            _inquiryRepository = inquiryRepository;
        }

        public async Task<VendorAnalyticsDto> Handle(GetVendorAnalyticsQuery request, CancellationToken cancellationToken)
        {
            var bookings = await _bookingRepository.GetBookingsByVendorUserIdAsync(request.VendorId, cancellationToken);
            var inquiries = await _inquiryRepository.GetByVendorIdAsync(request.VendorId, cancellationToken);

            var totalBookings = bookings.Count();
            var pendingBookings = bookings.Count(b => b.Status == MyWedding.Domain.Enums.BookingStatus.Pending);
            var totalEarnings = bookings
                .Where(b => b.Status == MyWedding.Domain.Enums.BookingStatus.Completed || b.Status == MyWedding.Domain.Enums.BookingStatus.Confirmed)
                .Sum(b => b.FinalAmount);

            var totalInquiries = inquiries.Count();
            var unreadInquiries = inquiries.Count(i => !i.IsRead);

            // Calculate earnings for the last 6 months
            var monthlyEarnings = new List<MonthlyEarningsDto>();
            var today = DateTime.UtcNow;
            
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = today.AddMonths(-i);
                var monthEarnings = bookings
                    .Where(b => (b.Status == MyWedding.Domain.Enums.BookingStatus.Completed || b.Status == MyWedding.Domain.Enums.BookingStatus.Confirmed) 
                                && b.ServiceDate.Year == targetMonth.Year 
                                && b.ServiceDate.Month == targetMonth.Month)
                    .Sum(b => b.FinalAmount);

                monthlyEarnings.Add(new MonthlyEarningsDto
                {
                    Month = targetMonth.ToString("MMM yyyy"),
                    Amount = monthEarnings
                });
            }

            return new VendorAnalyticsDto
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                TotalEarnings = totalEarnings,
                TotalInquiries = totalInquiries,
                UnreadInquiries = unreadInquiries,
                MonthlyEarnings = monthlyEarnings
            };
        }
    }
}
