using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorInquiryQuoteRepository : IVendorInquiryQuoteRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorInquiryQuoteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VendorInquiryQuote quote, CancellationToken cancellationToken = default)
        {
            await _context.VendorInquiryQuotes.AddAsync(quote, cancellationToken);
        }

        public async Task<VendorInquiryQuote?> GetLatestByInquiryIdAsync(
            Guid inquiryId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiryQuotes
                .AsNoTracking()
                .Where(q => q.InquiryId == inquiryId)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
