// File: src/Core/MyWedding.Domain/Interfaces/IEventOrganizerRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic; // <-- ADD THIS
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IEventOrganizerRepository
    {
        Task AddAsync(EventOrganizer organizer, CancellationToken cancellationToken = default);
        Task<bool> IsUserAlreadyOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default);
        Task<EventOrganizer?> GetOrganizerAsync(Guid eventId, string userId, CancellationToken cancellationToken = default);
        
        // --- NEW METHOD ---
        Task<IEnumerable<EventOrganizer>> GetOrganizersByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        
        void Update(EventOrganizer organizer);
        void Remove(EventOrganizer organizer);
    }
}
