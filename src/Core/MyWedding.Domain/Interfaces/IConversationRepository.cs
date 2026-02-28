// File: src/Core/MyWedding.Domain/Interfaces/IConversationRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IConversationRepository
    {
        Task<IEnumerable<Conversation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    }
}
