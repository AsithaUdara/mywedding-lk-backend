// File: src/Core/MyWedding.Domain/Interfaces/IEventTaskRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IEventTaskRepository
    {
        Task<EventTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EventTask>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<bool> AnyByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task AddAsync(EventTask task, CancellationToken cancellationToken = default);
        void Update(EventTask task); // Update is often synchronous in EF Core
        Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default);
    }
}