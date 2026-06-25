using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorAnalyticsRepository : IVendorAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorAnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<WeeklyViewCountPoint>> GetProfileViewsByWeekAsync(
            string vendorId,
            int weeks,
            CancellationToken cancellationToken = default)
        {
            var since = DateTime.UtcNow.Date.AddDays(-(weeks * 7));
            var views = await _context.VendorProfileViews
                .AsNoTracking()
                .Where(v => v.VendorId == vendorId && v.ViewedAt >= since)
                .Select(v => v.ViewedAt)
                .ToListAsync(cancellationToken);

            var points = new List<WeeklyViewCountPoint>();
            for (var w = weeks - 1; w >= 0; w--)
            {
                var weekStart = DateTime.UtcNow.Date.AddDays(-(w + 1) * 7 + 1);
                var weekEnd = weekStart.AddDays(7);
                var count = views.Count(d => d >= weekStart && d < weekEnd);
                points.Add(new WeeklyViewCountPoint
                {
                    Week = $"W{weeks - w}",
                    Views = count
                });
            }

            return points;
        }

        public async Task<IReadOnlyList<MonthlyCountPoint>> GetInquiriesByMonthAsync(
            string vendorId,
            int months,
            CancellationToken cancellationToken = default)
        {
            var inquiries = await _context.VendorInquiries
                .AsNoTracking()
                .Where(i => i.VendorId == vendorId)
                .Select(i => i.SentAt)
                .ToListAsync(cancellationToken);

            var points = new List<MonthlyCountPoint>();
            var today = DateTime.UtcNow;
            for (var i = months - 1; i >= 0; i--)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var count = inquiries.Count(d => d.Year == monthDate.Year && d.Month == monthDate.Month);
                points.Add(new MonthlyCountPoint
                {
                    Month = monthDate.ToString("MMM"),
                    Count = count
                });
            }

            return points;
        }

        public async Task<WinRateSummary> GetWinRateAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            var bookings = await _context.VendorBookings
                .AsNoTracking()
                .Join(
                    _context.VendorServices.Where(s => s.VendorId == vendorId),
                    b => b.ServiceId,
                    s => s.Id,
                    (b, _) => b.Status)
                .ToListAsync(cancellationToken);

            var won = bookings.Count(s =>
                s == BookingStatus.Confirmed || s == BookingStatus.Completed);
            var pending = bookings.Count(s =>
                s == BookingStatus.Requested
                || s == BookingStatus.Pending
                || s == BookingStatus.AwaitingPayment);
            var lost = bookings.Count(s => s == BookingStatus.Cancelled);

            return new WinRateSummary { Won = won, Pending = pending, Lost = lost };
        }
    }
}
