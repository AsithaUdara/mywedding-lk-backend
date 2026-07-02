using MediatR;
using MyWedding.Domain.Enums;
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
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IVendorRepository _vendorRepository;

        public GetVendorAnalyticsQueryHandler(
            IVendorBookingRepository bookingRepository,
            IVendorInquiryRepository inquiryRepository,
            IVendorServiceRepository serviceRepository,
            IVendorRepository vendorRepository)
        {
            _bookingRepository = bookingRepository;
            _inquiryRepository = inquiryRepository;
            _serviceRepository = serviceRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<VendorAnalyticsDto> Handle(GetVendorAnalyticsQuery request, CancellationToken cancellationToken)
        {
            var bookings  = await _bookingRepository.GetBookingsByVendorUserIdAsync(request.VendorId, cancellationToken);
            var inquiries = await _inquiryRepository.GetByVendorIdAsync(request.VendorId, cancellationToken);
            var services  = await _serviceRepository.GetByVendorIdAsync(request.VendorId, cancellationToken);
            var vendor    = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);

            // --- Services ---
            var serviceList   = services.ToList();
            var totalServices  = serviceList.Count;
            var activeServices = serviceList.Count(s => s.IsActive);

            // --- Bookings ---
            var bookingList       = bookings.ToList();
            var totalBookings     = bookingList.Count;
            var pendingBookings   = bookingList.Count(b => b.Status == BookingStatus.Pending);
            var confirmedBookings = bookingList.Count(b => b.Status == BookingStatus.Confirmed);
            var completedBookings = bookingList.Count(b => b.Status == BookingStatus.Completed);

            // --- Revenue ---
            var totalRevenue   = bookingList.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.FinalAmount);
            var pendingRevenue = bookingList.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.FinalAmount);

            // --- Ratings (from Vendor entity + Reviews navigation) ---
            var averageRating = vendor?.AverageRating ?? 0m;
            var totalReviews  = vendor?.Reviews?.Count ?? 0;

            // --- Inquiries ---
            var inquiryList    = inquiries.ToList();
            var totalInquiries  = inquiryList.Count;
            var unreadInquiries = inquiryList.Count(i => !i.IsRead);

            // --- Monthly earnings trend (last 6 months, completed + confirmed) ---
            var monthlyEarnings = new List<MonthlyEarningsDto>();
            var today = DateTime.UtcNow;

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth  = today.AddMonths(-i);
                var monthEarnings = bookingList
                    .Where(b => (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed)
                                && b.ServiceDate.Year  == targetMonth.Year
                                && b.ServiceDate.Month == targetMonth.Month)
                    .Sum(b => b.FinalAmount);

                monthlyEarnings.Add(new MonthlyEarningsDto
                {
                    Month  = targetMonth.ToString("MMM yyyy"),
                    Amount = monthEarnings
                });
            }

            return new VendorAnalyticsDto
            {
                TotalServices     = totalServices,
                ActiveServices    = activeServices,
                TotalBookings     = totalBookings,
                PendingBookings   = pendingBookings,
                ConfirmedBookings = confirmedBookings,
                CompletedBookings = completedBookings,
                TotalRevenue      = totalRevenue,
                PendingRevenue    = pendingRevenue,
                AverageRating     = averageRating,
                TotalReviews      = totalReviews,
                TotalInquiries    = totalInquiries,
                UnreadInquiries   = unreadInquiries,
                MonthlyEarnings   = monthlyEarnings
            };
        }
    }
}
