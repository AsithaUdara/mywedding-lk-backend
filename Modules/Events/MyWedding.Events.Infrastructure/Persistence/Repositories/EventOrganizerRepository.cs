// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/EventOrganizerRepository.cs
using Microsoft.EntityFrameworkCore;


using System;
using System.Collections.Generic; // <-- ADD THIS
using System.Linq;                // <-- ADD THIS
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Infrastructure.Persistence.Repositories
{
    public class EventOrganizerRepository : IEventOrganizerRepository
    {
        private readonly ApplicationDbContext _context;

        public EventOrganizerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EventOrganizer organizer, CancellationToken cancellationToken = default)
        {
            await _context.EventOrganizers.AddAsync(organizer, cancellationToken);
        }

        public async Task<bool> IsUserAlreadyOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.EventOrganizers
                .AsNoTracking()
                .AnyAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
        }
        
        public async Task<EventOrganizer?> GetOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.EventOrganizers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
        }

        // --- NEW IMPLEMENTATION ---
        public async Task<IEnumerable<EventOrganizer>> GetOrganizersByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            // Use .Include() to perform a JOIN and fetch the related User details
            return await _context.EventOrganizers
                .Include(o => o.User)
                .Where(o => o.EventId == eventId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public void Update(EventOrganizer organizer)
        {
            _context.EventOrganizers.Update(organizer);
        }

        public void Remove(EventOrganizer organizer)
        {
            _context.EventOrganizers.Remove(organizer);
        }
    }
}
