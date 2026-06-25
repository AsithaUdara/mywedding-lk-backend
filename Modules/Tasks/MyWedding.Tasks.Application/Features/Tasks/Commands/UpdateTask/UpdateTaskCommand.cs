using MediatR;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommand : IRequest
    {
        public Guid EventId { get; init; }
        public Guid TaskId { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DomainTaskStatus? Status { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? DueDate { get; init; }
        public Guid? DependsOnTaskId { get; init; }
        public bool UpdateDependency { get; init; }
        public string? UserId { get; set; }
    }
}
