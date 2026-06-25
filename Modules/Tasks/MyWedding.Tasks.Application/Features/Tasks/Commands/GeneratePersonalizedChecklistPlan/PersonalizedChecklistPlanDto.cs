namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GeneratePersonalizedChecklistPlan;

public record PersonalizedChecklistPlanDto(
    string ExecutiveSummary,
    IReadOnlyList<string> ExcludeTemplateTitles,
    IReadOnlyList<ProposedAdditionalTaskDto> AdditionalTasks,
    IReadOnlyList<ChecklistPreviewLineDto> TemplatePreview,
    bool IsSimulated
);

public record ProposedAdditionalTaskDto(
    string Title,
    string? Description,
    string Priority
);

public record ChecklistPreviewLineDto(
    int TemplateIndex,
    string Title,
    bool IncludedByDefault,
    bool ExcludedByPlan
);
