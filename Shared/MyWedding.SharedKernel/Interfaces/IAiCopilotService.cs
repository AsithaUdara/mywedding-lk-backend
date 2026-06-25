namespace MyWedding.SharedKernel.Interfaces;

/// <summary>
/// Planner co-pilot: AI-assisted communication and admin automation (see 05_AI_Integration_Strategy.md).
/// </summary>
public interface IAiCopilotService
{
    /// <summary>
    /// Drafts a personalized vendor inquiry email from event context.
    /// </summary>
    Task<InquiryEmailDraftResult> DraftInquiryEmailAsync(
        InquiryEmailDraftRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses meeting notes or transcript and proposes action items for the event Gantt.
    /// </summary>
    Task<MeetingSummaryResult> SummarizeMeetingToTasksAsync(
        MeetingSummaryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses couple brief + optional meeting notes to tailor the standard master checklist.
    /// </summary>
    Task<PersonalizedChecklistPlanResult> GeneratePersonalizedChecklistPlanAsync(
        PersonalizedChecklistPlanRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record InquiryEmailDraftRequest(
    string PlannerName,
    string VendorBusinessName,
    string VendorCategory,
    string EventName,
    DateOnly WeddingDate,
    string? Venue,
    decimal? BudgetLkr,
    string? StyleNotes,
    IReadOnlyList<string>? ServiceRequirements);

public sealed record InquiryEmailDraftResult(
    string Subject,
    string Body,
    bool IsSimulated);

public sealed record MeetingSummaryRequest(
    Guid EventId,
    string EventName,
    string MeetingNotesOrTranscript);

public sealed record ProposedTaskItem(
    string Title,
    string? Description,
    DateOnly? SuggestedDueDate,
    string Priority);

public sealed record MeetingSummaryResult(
    string ExecutiveSummary,
    IReadOnlyList<ProposedTaskItem> ProposedTasks,
    bool IsSimulated);

public sealed record PersonalizedChecklistPlanRequest(
    Guid EventId,
    string EventName,
    DateOnly WeddingDate,
    int? EstimatedGuestCount,
    int? GuestCountMax,
    string? WeddingStyle,
    string? VenuePreference,
    string? MustHavesNotes,
    string? ServicesAlreadyBooked,
    string? CulturalOrReligiousNotes,
    decimal? BudgetLkr,
    string? MeetingNotesOrTranscript,
    IReadOnlyList<string> StandardTemplateTaskTitles);

public sealed record PersonalizedChecklistPlanResult(
    string ExecutiveSummary,
    IReadOnlyList<string> ExcludeTemplateTitles,
    IReadOnlyList<ProposedTaskItem> AdditionalTasks,
    bool IsSimulated);
