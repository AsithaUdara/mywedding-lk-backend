using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IEventInvitationRepository
    {
        Task AddAsync(EventInvitation invitation, CancellationToken cancellationToken = default);
        Task<EventInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IEnumerable<EventInvitation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        void Update(EventInvitation invitation);
    }
}
