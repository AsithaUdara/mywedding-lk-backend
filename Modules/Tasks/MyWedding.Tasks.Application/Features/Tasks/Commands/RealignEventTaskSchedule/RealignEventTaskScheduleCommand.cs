using MediatR;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.RealignEventTaskSchedule;

public class RealignEventTaskScheduleCommand : IRequest<RealignEventTaskScheduleResult>
{
    public Guid EventId { get; set; }
    public string? UserId { get; set; }
}

public record RealignEventTaskScheduleResult(int TasksUpdated, int TasksSkipped);
