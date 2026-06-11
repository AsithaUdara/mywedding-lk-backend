using MyWedding.Domain.Entities;

namespace MyWedding.Events.Application.Features.EventBrief;

internal static class EventBriefMapper
{
    public static EventBriefDto ToDto(WeddingEvent weddingEvent)
    {
        var completion = CalculateCompletionPercent(weddingEvent);
        return new EventBriefDto(
            weddingEvent.Id,
            weddingEvent.EventName,
            weddingEvent.EventDate,
            weddingEvent.TotalBudget,
            weddingEvent.TaskPlanPhase.ToString(),
            weddingEvent.EventLifecycleStage.ToString(),
            weddingEvent.EstimatedGuestCount,
            weddingEvent.GuestCountMax,
            weddingEvent.WeddingStyle,
            weddingEvent.VenuePreference,
            weddingEvent.MustHavesNotes,
            weddingEvent.ServicesAlreadyBooked,
            weddingEvent.CulturalOrReligiousNotes,
            weddingEvent.BriefCompletedAt,
            weddingEvent.BriefCompletedAt.HasValue || completion >= 80,
            completion
        );
    }

    public static int CalculateCompletionPercent(WeddingEvent weddingEvent)
    {
        var score = 0;
        if (weddingEvent.EstimatedGuestCount is > 0) score += 20;
        if (!string.IsNullOrWhiteSpace(weddingEvent.WeddingStyle)) score += 20;
        if (!string.IsNullOrWhiteSpace(weddingEvent.VenuePreference)) score += 15;
        if (!string.IsNullOrWhiteSpace(weddingEvent.MustHavesNotes)) score += 25;
        if (weddingEvent.TotalBudget > 0) score += 10;
        if (!string.IsNullOrWhiteSpace(weddingEvent.ServicesAlreadyBooked)) score += 10;
        return Math.Min(100, score);
    }
}
