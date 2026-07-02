namespace MyWedding.SharedKernel.Interfaces;

public interface IEventAiService
{
    Task<EventAiWorkflowResult> ChatAsync(
        string? userId,
        Guid eventId,
        string message,
        CancellationToken cancellationToken = default);

    Task<EventAiWorkflowResult> RecommendVendorsAsync(
        string? userId,
        Guid eventId,
        int topN,
        CancellationToken cancellationToken = default);

    Task<EventAiWorkflowResult> GenerateItineraryAsync(
        string? userId,
        Guid eventId,
        int defaultServiceDurationHours,
        CancellationToken cancellationToken = default);

    Task<EventAiWorkflowResult> GetItineraryAsync(
        string? userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<EventAiWorkflowResult> SaveItineraryAsync(
        string? userId,
        Guid itineraryId,
        IReadOnlyCollection<EventAiSaveItineraryItem> items,
        CancellationToken cancellationToken = default);
}

public record EventAiWorkflowResult(int StatusCode, object? Body);

public record EventAiVendorRecommendationDto(string VendorId, string BusinessName, decimal Score, string Reason);

public record EventAiSaveItineraryItem(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);
