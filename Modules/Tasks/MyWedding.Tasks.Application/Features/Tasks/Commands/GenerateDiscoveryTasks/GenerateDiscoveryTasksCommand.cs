using MediatR;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateDiscoveryTasks;

public class GenerateDiscoveryTasksCommand : IRequest<int>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
    public bool SkipIfDiscoveryExists { get; set; } = true;
}
