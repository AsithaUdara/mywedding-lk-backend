// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/WeddingEventRepository.cs
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MyWedding.Infrastructure.Persistence.Repositories
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
            return await _context.WeddingEvents
                .Where(e => e.CreatedById == userId)
                .ToListAsync(cancellationToken);
        }
    }
}
