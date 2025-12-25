// File: src/Core/MyWedding.Domain/Interfaces/IEventOrganizerRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IEventOrganizerRepository
    {
        Task AddAsync(EventOrganizer organizer, CancellationToken cancellationToken = default);
        Task<bool> IsUserAlreadyOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default);
        Task<EventOrganizer?> GetOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default);
    }
}
