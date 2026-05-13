// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventById/GetEventByIdQuery.cs
using MediatR;
using System;

namespace MyWedding.Events.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQuery : IRequest<EventDto?>
    {
        public Guid EventId { get; init; }
    }
}
