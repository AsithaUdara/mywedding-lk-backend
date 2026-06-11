// File: src/Core/MyWedding.Domain/Interfaces/IWeddingEventRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace MyWedding.Domain.Interfaces
{
    public interface IWeddingEventRepository
    {
        Task AddAsync(WeddingEvent weddingEvent, CancellationToken cancellationToken = default);
        void Update(WeddingEvent weddingEvent);
        Task<WeddingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<WeddingEvent?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> IsManagedByPlannerAsync(Guid eventId, string plannerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<WeddingEvent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
