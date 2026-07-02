using MediatR;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

public class GenerateTaskTemplateCommand : IRequest<int>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
    public bool SkipIfTasksExist { get; set; } = true;
}
