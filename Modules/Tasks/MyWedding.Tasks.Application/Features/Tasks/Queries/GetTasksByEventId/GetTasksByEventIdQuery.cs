// File: src/Core/MyWedding.Application/Features/Tasks/Queries/GetTasksByEventId/GetTasksByEventIdQuery.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetTasksByEventId
{
    public class GetTasksByEventIdQuery : IRequest<IEnumerable<TaskDto>>
    {
        public Guid EventId { get; init; }
        public string? UserId { get; set; }
    }
}
