using MediatR;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.ApplyPersonalizedChecklist;

public class ApplyPersonalizedChecklistCommand : IRequest<ApplyPersonalizedChecklistResult>
{
    public Guid EventId { get; set; }
    public required string UserId { get; set; }
    public IReadOnlyList<string> ExcludeTemplateTitles { get; set; } = [];
    public IReadOnlyList<AdditionalChecklistTask> AdditionalTasks { get; set; } = [];
    public bool MarkBriefComplete { get; set; } = true;
}

public record ApplyPersonalizedChecklistResult(int TasksCreated, string TaskPlanPhase, string Message);
