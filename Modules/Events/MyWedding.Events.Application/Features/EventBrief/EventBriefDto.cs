namespace MyWedding.Events.Application.Features.EventBrief;

public record EventBriefDto(
    Guid EventId,
    string EventName,
    DateTime EventDate,
    decimal TotalBudget,
    string TaskPlanPhase,
    string EventLifecycleStage,
    int? EstimatedGuestCount,
    int? GuestCountMax,
    string? WeddingStyle,
    string? VenuePreference,
    string? MustHavesNotes,
    string? ServicesAlreadyBooked,
    string? CulturalOrReligiousNotes,
    DateTime? BriefCompletedAt,
    bool IsBriefComplete,
    int BriefCompletionPercent
);
