using MediatR;
using System;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.AssignTask;

public class AssignTaskCommand : IRequest
{
    public Guid TaskId { get; init; }
    public string? AssignedToUserId { get; init; }
    public required string UserId { get; init; }
}
