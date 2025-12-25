// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/EventOrganizerRepository.cs
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
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
                .AnyAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
        }

        public async Task<EventOrganizer?> GetOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.EventOrganizers
                .FirstOrDefaultAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
        }
    }
}
