using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default)
            => _context.Users.CountAsync(cancellationToken);

        public Task<int> GetTotalVendorsAsync(CancellationToken cancellationToken = default)
            => _context.Vendors.CountAsync(cancellationToken);

        public Task<int> GetTotalEventsAsync(CancellationToken cancellationToken = default)
            => _context.WeddingEvents.CountAsync(cancellationToken);

        public Task<int> GetTotalBookingsAsync(CancellationToken cancellationToken = default)
            => _context.VendorBookings.CountAsync(cancellationToken);
    }
}
