// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventsByUserId/GetEventsByUserIdQueryHandler.cs
using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Events.Queries.GetEventsByUserId
{
    public class GetEventsByUserIdQueryHandler : IRequestHandler<GetEventsByUserIdQuery, IEnumerable<EventDto>>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;
        private readonly IEventOrganizerRepository _organizerRepository;

        public GetEventsByUserIdQueryHandler(
            IWeddingEventRepository weddingEventRepository,
            IEventOrganizerRepository organizerRepository)
        {
            _weddingEventRepository = weddingEventRepository;
            _organizerRepository = organizerRepository;
        }

        public async Task<IEnumerable<EventDto>> Handle(GetEventsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var weddingEvents = await _weddingEventRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            var result = new List<EventDto>();
            foreach (var e in weddingEvents)
            {
                var canBook = await CanUserBookForEventAsync(e.Id, e.CreatedById, request.UserId, cancellationToken);
                result.Add(new EventDto(
                    e.Id,
                    e.EventName,
                    e.EventDate,
                    e.CreatedById,
                    e.TotalBudget,
                    canBook));
            }

            return result;
        }

        private async Task<bool> CanUserBookForEventAsync(
            Guid eventId,
            string createdById,
            string userId,
            CancellationToken cancellationToken)
        {
            if (createdById == userId)
                return true;

            var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId, cancellationToken);
            return organizer is not null && organizer.PermissionLevel != PermissionLevel.Viewer;
        }
    }
}
