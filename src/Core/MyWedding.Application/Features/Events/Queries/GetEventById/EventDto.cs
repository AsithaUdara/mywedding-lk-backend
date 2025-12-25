// File: src/Core/MyWedding.Application/Features/Events/Queries/GetEventById/EventDto.cs
using System;

namespace MyWedding.Application.Features.Events.Queries.GetEventById
{
    public record EventDto(Guid Id, string EventName, DateTime EventDate, string CreatedById);
}
