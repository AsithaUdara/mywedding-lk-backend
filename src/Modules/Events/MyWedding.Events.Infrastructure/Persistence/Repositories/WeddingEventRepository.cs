// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/WeddingEventRepository.cs
using Microsoft.EntityFrameworkCore;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MyWedding.Events.Infrastructure.Persistence.Repositories
{
    public class WeddingEventRepository : IWeddingEventRepository
    {
        private readonly ApplicationDbContext _context;

        public WeddingEventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WeddingEvent weddingEvent, CancellationToken cancellationToken = default)
        {
            await _context.WeddingEvents.AddAsync(weddingEvent, cancellationToken);
        }

        public async Task<WeddingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.WeddingEvents.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<IEnumerable<WeddingEvent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            // Retrieve events where the user is an organizer (this includes the creator, as they are added as an organizer too)
            return await _context.EventOrganizers
                .AsNoTracking()
                .Include(eo => eo.WeddingEvent)
                .Where(eo => eo.UserId == userId)
                .Select(eo => eo.WeddingEvent!) // null-forgiving operator as Include ensures it's loaded
                .ToListAsync(cancellationToken);
        }
    }
}
