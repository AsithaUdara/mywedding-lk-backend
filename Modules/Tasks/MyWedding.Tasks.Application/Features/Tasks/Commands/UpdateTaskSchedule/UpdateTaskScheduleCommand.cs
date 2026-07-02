using MediatR;
using System;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.UpdateTaskSchedule
{
    public class UpdateTaskScheduleCommand : IRequest
    {
        public Guid EventId { get; init; }
        public Guid TaskId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? DueDate { get; init; }
        public Guid? DependsOnTaskId { get; init; }
        public bool UpdateDependency { get; init; }
        public string? UserId { get; set; }
    }
}
