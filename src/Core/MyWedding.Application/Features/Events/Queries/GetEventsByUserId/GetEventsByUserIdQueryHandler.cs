// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventsByUserId/GetEventsByUserIdQueryHandler.cs
using MediatR;
using MyWedding.Application.Features.Events.Queries.GetEventById;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Events.Queries.GetEventsByUserId
{
    public class GetEventsByUserIdQueryHandler : IRequestHandler<GetEventsByUserIdQuery, IEnumerable<EventDto>>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;

        public GetEventsByUserIdQueryHandler(IWeddingEventRepository weddingEventRepository)
        {
            _weddingEventRepository = weddingEventRepository;
        }

        public async Task<IEnumerable<EventDto>> Handle(GetEventsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var weddingEvents = await _weddingEventRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            // Map the list of entities to a list of DTOs
            return weddingEvents.Select(e => new EventDto(
                e.Id,
                e.EventName,
                e.EventDate,
                e.CreatedById));
        }
    }
}
