// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/ConversationRepository.cs

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
    public class ConversationRepository : IConversationRepository
    {
        private readonly ApplicationDbContext _context;

        public ConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            await _context.Conversations.AddAsync(conversation, cancellationToken);
        }

        public async Task<IEnumerable<Conversation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Where(c => c.EventId == eventId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
    }
}
