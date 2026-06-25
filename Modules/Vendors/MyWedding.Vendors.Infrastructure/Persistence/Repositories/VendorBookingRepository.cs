using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.Vendors.Infrastructure.Persistence.Repositories
{
    public class VendorBookingRepository : IVendorBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorBookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VendorBooking booking, CancellationToken cancellationToken = default)
        {
            await _context.VendorBookings.AddAsync(booking, cancellationToken);
        }

        public async Task<bool> HasBookingsAsync(Guid serviceId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorBookings.AnyAsync(b => b.ServiceId == serviceId, cancellationToken);
        }

        public async Task<System.Collections.Generic.IEnumerable<VendorBooking>> GetBookingsByVendorUserIdAsync(string vendorUserId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorBookings
                .Include(b => b.VendorService)
                    .ThenInclude(vs => vs!.Vendor)
                .Include(b => b.WeddingEvent)
                .Include(b => b.BookedBy)
                .Where(b => b.VendorService != null && b.VendorService.Vendor != null && b.VendorService.Vendor.UserId == vendorUserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<System.Collections.Generic.IEnumerable<VendorBooking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorBookings
                .Include(b => b.VendorService)
                    .ThenInclude(vs => vs!.Vendor)
                .Where(b => b.EventId == eventId)
                .ToListAsync(cancellationToken);
        }

        public async Task<VendorBooking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.VendorBookings
                .Include(b => b.VendorService)
                    .ThenInclude(vs => vs!.Vendor)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<int>> GetBookedDaysForVendorInMonthAsync(
            string vendorId,
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            return await _context.VendorBookings
                .AsNoTracking()
                .Join(
                    _context.VendorServices.Where(s => s.VendorId == vendorId),
                    b => b.ServiceId,
                    s => s.Id,
                    (b, _) => b)
                .Where(b =>
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                    && b.ServiceDate >= monthStart
                    && b.ServiceDate < monthEnd)
                .Select(b => b.ServiceDate.Day)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(cancellationToken);
        }
    }
}
