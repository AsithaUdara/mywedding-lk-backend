// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventsByUserId/GetEventsByUserIdQuery.cs
using MediatR;
// Re-use the EventDto
using System.Collections.Generic;

namespace MyWedding.Events.Application.Features.Events.Queries.GetEventsByUserId
{
    public class GetEventsByUserIdQuery : IRequest<IEnumerable<EventDto>>
    {
        public required string UserId { get; init; }
    }
}
