using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Security;
using System;
using System.Threading.Tasks;

namespace MyWedding.Events.Infrastructure.Security
{
    public class EventAuthorizationService : IEventAuthorizationService
    {
        private readonly IEventOrganizerRepository _organizerRepository;

        public EventAuthorizationService(IEventOrganizerRepository organizerRepository)
        {
            _organizerRepository = organizerRepository;
        }

        public async Task<bool> IsOrganizerAsync(Guid eventId, string userId)
        {
            return await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId);
        }
    }
}
