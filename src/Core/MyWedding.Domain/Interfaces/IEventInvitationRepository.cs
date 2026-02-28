using MyWedding.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IEventInvitationRepository
    {
        Task AddAsync(EventInvitation invitation, CancellationToken cancellationToken = default);
        Task<EventInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        void Update(EventInvitation invitation);
    }
}
