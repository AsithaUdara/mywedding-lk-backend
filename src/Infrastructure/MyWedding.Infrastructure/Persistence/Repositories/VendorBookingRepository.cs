using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
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
                .Include(b => b.BookingContract)
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
                .Include(b => b.BookingContract)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }
    }
}
