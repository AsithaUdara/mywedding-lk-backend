using MediatR;
using System;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.DeleteTask
{
    public class DeleteTaskCommand : IRequest
    {
        public Guid EventId { get; init; }
        public Guid TaskId { get; init; }
        public string? UserId { get; set; }
    }
}
