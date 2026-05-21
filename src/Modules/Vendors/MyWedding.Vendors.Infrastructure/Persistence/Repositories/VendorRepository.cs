using Microsoft.EntityFrameworkCore;


using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Infrastructure.Persistence.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Vendor?> GetByIdAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            // Use .Include() to eagerly load all related data needed for the detail page
            return await _context.Vendors
                .Include(v => v.User) // Join with Users table to get email
                .Include(v => v.Services)
                    .ThenInclude(s => s.Category) // Also join the Category for each Service
                .Include(v => v.Reviews)
                    .ThenInclude(r => r.Reviewer) // Join with Users table again to get the reviewer's name
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == vendorId, cancellationToken);
        }

        public async Task<IEnumerable<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Implementation...
            IQueryable<Vendor> query = _context.Vendors
                .Include(v => v.User)
                .Include(v => v.Services)
                    .ThenInclude(s => s.Category)
                .AsNoTracking();

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Vendor>> GetPendingVendorsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .Where(v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Pending)
                .Include(v => v.User)
                .Include(v => v.PrimaryCategory)
                .AsNoTracking()
                .OrderBy(v => v.BusinessName)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
        {
            await _context.Vendors.AddAsync(vendor, cancellationToken);
        }

        public void Update(Vendor vendor)
        {
            _context.Vendors.Update(vendor);
        }
    }
}
