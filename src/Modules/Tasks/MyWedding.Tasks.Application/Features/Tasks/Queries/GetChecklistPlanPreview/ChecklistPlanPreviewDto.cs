namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetChecklistPlanPreview;

public record ChecklistPlanPreviewDto(
    Guid EventId,
    string EventName,
    DateTime WeddingDate,
    string TaskPlanPhase,
    bool IsBriefComplete,
    int BriefCompletionPercent,
    int DiscoveryTaskCount,
    int ExistingTaskCount,
    bool CanGenerateFullChecklist,
    IReadOnlyList<ChecklistPreviewTaskDto> TemplateTasks,
    EventBriefSummaryDto Brief
);

public record ChecklistPreviewTaskDto(
    int TemplateIndex,
    string Title,
    DateTime StartDate,
    DateTime DueDate,
    bool IncludedByDefault,
    bool ExcludedByAi
);

public record EventBriefSummaryDto(
    int? EstimatedGuestCount,
    int? GuestCountMax,
    string? WeddingStyle,
    string? VenuePreference,
    string? MustHavesNotes,
    string? ServicesAlreadyBooked,
    string? CulturalOrReligiousNotes
);
