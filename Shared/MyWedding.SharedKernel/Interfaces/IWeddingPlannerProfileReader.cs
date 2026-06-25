namespace MyWedding.SharedKernel.Interfaces;

public record WeddingPlannerProfileSnapshot(
    string DisplayName,
    string BusinessName,
    string? AgencyLogoUrl);

public record EventPlannerBrandingSnapshot(
    string BusinessName,
    string DisplayName,
    string? AgencyLogoUrl,
    bool IsWhiteLabeled);

public interface IWeddingPlannerProfileReader
{
    Task<WeddingPlannerProfileSnapshot?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<EventPlannerBrandingSnapshot?> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
