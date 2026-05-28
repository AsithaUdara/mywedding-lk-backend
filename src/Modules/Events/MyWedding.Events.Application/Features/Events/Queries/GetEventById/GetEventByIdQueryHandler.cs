// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventById/GetEventByIdQueryHandler.cs
using MediatR;

using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
    {
        private readonly IWeddingEventRepository _weddingEventRepository;

        public GetEventByIdQueryHandler(IWeddingEventRepository weddingEventRepository)
        {
            _weddingEventRepository = weddingEventRepository;
        }

        public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var weddingEvent = await _weddingEventRepository.GetByIdAsync(request.EventId, cancellationToken);

            if (weddingEvent is null)
            {
                return null; // Event not found
            }

            // Map the entity to the DTO
            return new EventDto(
                weddingEvent.Id,
                weddingEvent.EventName,
                weddingEvent.EventDate,
                weddingEvent.CreatedById,
                weddingEvent.TotalBudget);
        }
    }
}
