namespace MyWedding.SharedKernel.Interfaces;

public interface IPlannerWorkspaceService
{
    Task<PlannerWorkflowResult> SignupAsync(string? userId, PlannerSignupRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetDashboardAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetOverviewAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetClientsAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetPlannerEventsAsync(string? userId, string? status, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetPlannerBookingsAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> UpdateEventStageAsync(Guid eventId, UpdateEventLifecycleStageRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> CreatePlannerEventAsync(string? userId, CreatePlannerEventRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> AssignClientAsync(string? userId, Guid eventId, AssignPlannerClientRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> CheckPlannerEventAccessAsync(string? userId, Guid eventId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> UpdateSubscriptionAsync(string? userId, UpdatePlannerSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> GetBillingProfileAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> SaveBillingProfileAsync(string? userId, PlannerBillingProfileRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> UpdateProfileAsync(string? userId, UpdatePlannerProfileRequest request, CancellationToken cancellationToken = default);

    Task<PlannerWorkflowResult> UpdateAgencyLogoAsync(string? userId, UpdatePlannerAgencyLogoRequest request, CancellationToken cancellationToken = default);
}

public record PlannerWorkflowResult(int StatusCode, object? Body);

public record PlannerSignupRequest(string BusinessName, string? BusinessDescription, string? ContactPhone, string? City);

public record CreatePlannerEventRequest(
    string EventName,
    DateTime EventDate,
    decimal TotalBudget,
    string? ClientUserId,
    string? ClientEmail,
    string TaskSeedMode = "DiscoveryStarter",
    Guid? CustomTemplateId = null);

public record AssignPlannerClientRequest(string? ClientUserId, string? ClientEmail);

public record UpdatePlannerSubscriptionRequest(string Tier, decimal MonthlyFee);

public record UpdatePlannerProfileRequest(string BusinessName, string? BusinessDescription, string? ContactPhone, string? City);

public record UpdatePlannerAgencyLogoRequest(string AgencyLogoUrl);

public record PlannerClientEventSummary(Guid PlannerClientEventId, Guid EventId, string EventName, DateTime EventDate, string ClientUserId, string ClientEmail, string Status);

public record PlannerUpcomingEventDto(Guid EventId, string EventName, DateTime EventDate, string ClientEmail, string Status, decimal TotalBudget);

public record PlannerClientDto(string ClientUserId, string ClientEmail, int TotalEvents, int ActiveEvents, DateTime LastActivityAt);

public record UpdateEventLifecycleStageRequest(string Stage);

public record PlannerBookingListItemDto(
    Guid BookingId,
    Guid EventId,
    string EventName,
    string ServiceName,
    string VendorName,
    string BookingStatus,
    string PaymentStatus,
    decimal FinalAmount,
    DateTime CreatedAt
);

public record PlannerEventListItemDto(
    Guid PlannerClientEventId,
    Guid EventId,
    string EventName,
    DateTime EventDate,
    string ClientUserId,
    string ClientEmail,
    string Status,
    decimal TotalBudget,
    decimal SpentBudget,
    int RequestedBookings,
    int ConfirmedBookings,
    int CompletedBookings,
    string EventLifecycleStage,
    string TaskPlanPhase
);

public record PlannerOverviewResponse(
    string PlannerId,
    string PlannerName,
    string BusinessName,
    string? BusinessDescription,
    string? City,
    string ActivePlanTier,
    int MaxConcurrentEvents,
    int ActiveWeddings,
    int PendingBookings,
    int ConfirmedBookings,
    IReadOnlyCollection<PlannerUpcomingEventDto> UpcomingEvents,
    decimal SubscriptionMonthlyFee,
    DateTime? SubscriptionEndsAt,
    DateTime? SubscriptionStartsAt
);

public record PlannerBillingProfileRequest(
    string? CardholderName,
    string? CardBrand,
    string Last4,
    byte? ExpiryMonth,
    short? ExpiryYear);

public record PlannerDashboardResponse(
    string PlannerId,
    string PlannerName,
    string BusinessName,
    string? BusinessDescription,
    string? ContactPhone,
    string? City,
    string ActivePlanTier,
    int MaxConcurrentEvents,
    IReadOnlyCollection<PlannerClientEventSummary> Events,
    string? AgencyLogoUrl
);
