// File: src/Core/MyWedding.Domain/Interfaces/IMessageRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
        Task AddAsync(Message message, CancellationToken cancellationToken = default);
    }
}
