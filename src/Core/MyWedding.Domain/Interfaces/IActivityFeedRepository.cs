// File: src/Core/MyWedding.Domain/Interfaces/IActivityFeedRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IActivityFeedRepository
    {
        Task AddAsync(ActivityFeedItem item, CancellationToken cancellationToken = default);
        Task<IEnumerable<ActivityFeedItem>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    }
}
