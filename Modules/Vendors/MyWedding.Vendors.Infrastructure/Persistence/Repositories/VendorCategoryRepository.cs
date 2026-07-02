using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;

namespace MyWedding.Vendors.Infrastructure.Persistence.Repositories
{
    public class VendorCategoryRepository : IVendorCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<VendorCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        {
            return await _context.VendorCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
