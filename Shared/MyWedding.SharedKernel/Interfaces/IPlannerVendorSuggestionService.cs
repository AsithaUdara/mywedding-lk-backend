namespace MyWedding.SharedKernel.Interfaces;

public interface IPlannerVendorSuggestionService
{
    Task<IReadOnlyList<PlannerVendorSuggestionDto>> BuildSuggestionsAsync(
        Guid eventId,
        string category,
        int topN,
        CancellationToken cancellationToken = default);
}

public record PlannerVendorSuggestionDto(
    Guid VendorServiceId,
    string VendorUserId,
    string BusinessName,
    string ServiceName,
    string CategoryName,
    decimal BasePrice,
    decimal AverageRating,
    decimal Score,
    string Reason);
