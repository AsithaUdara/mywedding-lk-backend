// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/ActivityFeedRepository.cs
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
    public class ActivityFeedRepository : IActivityFeedRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityFeedRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ActivityFeedItem item, CancellationToken cancellationToken = default)
        {
            await _context.ActivityFeedItems.AddAsync(item, cancellationToken);
        }

        public async Task<IEnumerable<ActivityFeedItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.ActivityFeedItems
                .Include(i => i.User)
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
