// File: src/Core/MyWedding.Application/Features/Tasks/Commands/UpdateTaskStatus/UpdateTaskStatusCommand.cs
using MediatR;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System;

namespace MyWedding.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public class UpdateTaskStatusCommand : IRequest
    {
        public Guid TaskId { get; init; }
        public DomainTaskStatus NewStatus { get; init; }
        public required string UserId { get; init; }
    }
}