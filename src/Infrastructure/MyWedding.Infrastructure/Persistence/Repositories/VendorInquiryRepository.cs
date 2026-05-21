using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorInquiryRepository : IVendorInquiryRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorInquiryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VendorInquiry inquiry, CancellationToken cancellationToken = default)
        {
            await _context.VendorInquiries.AddAsync(inquiry, cancellationToken);
        }

        public async Task<IEnumerable<VendorInquiry>> GetByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiries
                .Where(vi => vi.VendorId == vendorId)
                .OrderByDescending(vi => vi.SentAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<VendorInquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiries.FindAsync(new object[] { id }, cancellationToken);
        }
    }
}
