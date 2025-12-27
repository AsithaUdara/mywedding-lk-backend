using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
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
            // This method will perform the filtering and sorting on the DATABASE side
            // For now, it returns all vendors, but it's ready for query parameters
            IQueryable<Vendor> query = _context.Vendors
                .Include(v => v.User)
                .Include(v => v.Services)
                    .ThenInclude(s => s.Category)
                .AsNoTracking();

            // Example of how backend filtering will work (we will use this in the Application layer later)
            // if (!string.IsNullOrEmpty(categoryFilter))
            // {
            //     query = query.Where(v => v.Services.Any(s => s.Category.Name == categoryFilter));
            // }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
