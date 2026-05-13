using Microsoft.EntityFrameworkCore;


using System;
using System.Threading;
using System.Threading.Tasks;

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
    }
}
