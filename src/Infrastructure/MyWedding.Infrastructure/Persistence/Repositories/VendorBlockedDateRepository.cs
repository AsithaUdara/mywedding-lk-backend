using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorBlockedDateRepository : IVendorBlockedDateRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorBlockedDateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<VendorBlockedDate>> GetByVendorAndMonthAsync(
            string vendorId,
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            return await _context.VendorBlockedDates
                .AsNoTracking()
                .Where(b => b.VendorId == vendorId && b.Date >= start && b.Date < end)
                .OrderBy(b => b.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<VendorBlockedDate?> GetByVendorAndDateAsync(
            string vendorId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            return await _context.VendorBlockedDates
                .FirstOrDefaultAsync(
                    b => b.VendorId == vendorId && b.Date == day,
                    cancellationToken);
        }

        public async Task AddAsync(VendorBlockedDate blockedDate, CancellationToken cancellationToken = default)
        {
            await _context.VendorBlockedDates.AddAsync(blockedDate, cancellationToken);
        }

        public Task DeleteAsync(VendorBlockedDate blockedDate, CancellationToken cancellationToken = default)
        {
            _context.VendorBlockedDates.Remove(blockedDate);
            return Task.CompletedTask;
        }
    }
}
