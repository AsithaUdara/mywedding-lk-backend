using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorProfileViewRepository : IVendorProfileViewRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorProfileViewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordViewAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            await _context.VendorProfileViews.AddAsync(new VendorProfileView
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                ViewedAt = DateTime.UtcNow
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
