using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLogItem item, CancellationToken cancellationToken = default)
        {
            await _context.AuditLogItems.AddAsync(item, cancellationToken);
        }

        public async Task<IEnumerable<AuditLogItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogItems
                .Include(i => i.Actor)
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.TimestampUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
