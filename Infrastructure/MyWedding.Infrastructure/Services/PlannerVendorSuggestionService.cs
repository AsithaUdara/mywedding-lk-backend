using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class PlannerVendorSuggestionService : IPlannerVendorSuggestionService
{
    private readonly ApplicationDbContext _db;

    public PlannerVendorSuggestionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlannerVendorSuggestionDto>> BuildSuggestionsAsync(
        Guid eventId,
        string category,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var weddingEvent = await _db.WeddingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (weddingEvent is null)
            return [];

        var expenseTotal = await _db.Expenses
            .Where(e => e.EventId == weddingEvent.Id)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var remainingBudget = Math.Max(0, weddingEvent.TotalBudget - expenseTotal);

        var categoryKeywords = ResolveCategoryKeywords(category);
        var weddingDay = weddingEvent.EventDate.Date;

        var existingShortlistServiceIds = await _db.VendorShortlistItems
            .AsNoTracking()
            .Where(s => s.EventId == weddingEvent.Id)
            .Select(s => s.VendorServiceId)
            .ToListAsync(cancellationToken);

        var bookedServiceIds = await _db.VendorBookings
            .AsNoTracking()
            .Where(b =>
                b.ServiceDate.Date == weddingDay &&
                b.Status == BookingStatus.Confirmed)
            .Select(b => b.ServiceId)
            .ToListAsync(cancellationToken);

        var bookedVendorIds = await _db.VendorServices
            .AsNoTracking()
            .Where(s => bookedServiceIds.Contains(s.Id))
            .Select(s => s.VendorId)
            .ToListAsync(cancellationToken);

        var blockedVendorIds = await _db.VendorBlockedDates
            .AsNoTracking()
            .Where(b => b.Date.Date == weddingDay)
            .Select(b => b.VendorId)
            .ToListAsync(cancellationToken);

        var unavailableVendorIds = bookedVendorIds
            .Concat(blockedVendorIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var styleTokens = Tokenize(weddingEvent.WeddingStyle);
        var venueTokens = Tokenize(weddingEvent.VenuePreference);

        var services = await _db.VendorServices
            .AsNoTracking()
            .Include(s => s.Vendor)
            .Include(s => s.Category)
            .Where(s =>
                s.IsActive &&
                s.Vendor != null &&
                s.Vendor.VerificationStatus == VerificationStatus.Verified &&
                !existingShortlistServiceIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return services
            .Where(s =>
                s.Vendor != null &&
                !unavailableVendorIds.Contains(s.Vendor.UserId) &&
                MatchesCategory(s, categoryKeywords))
            .Select(s =>
            {
                var vendor = s.Vendor!;
                var categoryName = s.Category?.Name ?? "Other";
                var priceFitness = remainingBudget <= 0
                    ? 0.6m
                    : s.BasePrice <= remainingBudget
                        ? 1m
                        : s.BasePrice <= remainingBudget * 1.25m
                            ? 0.7m
                            : 0.35m;
                var ratingFitness = vendor.AverageRating <= 0
                    ? 0.55m
                    : Math.Min(vendor.AverageRating / 5m, 1m);
                var styleFitness = ComputeStyleFitness(
                    styleTokens,
                    venueTokens,
                    vendor.BusinessDescription,
                    s.ServiceDescription,
                    s.ServiceName);

                var score = Math.Round(
                    (priceFitness * 0.35m) + (ratingFitness * 0.35m) + (styleFitness * 0.30m),
                    3);

                var reason = BuildReason(
                    categoryName,
                    s.BasePrice,
                    remainingBudget,
                    vendor.AverageRating,
                    styleFitness,
                    vendor.City);

                return new PlannerVendorSuggestionDto(
                    s.Id,
                    vendor.UserId,
                    vendor.BusinessName,
                    s.ServiceName,
                    categoryName,
                    s.BasePrice,
                    vendor.AverageRating,
                    score,
                    reason);
            })
            .GroupBy(s => s.VendorUserId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(s => s.Score)
            .Take(topN)
            .ToList();
    }

    private static IReadOnlyList<string> ResolveCategoryKeywords(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            "photographers" or "photography" => ["photography", "photographer"],
            "videographers" or "videography" => ["videography", "videographer", "photography"],
            "venues" or "venue" => ["venue"],
            "caterers" or "catering" => ["catering", "caterer"],
            "music" or "music & dj" => ["music", "dj"],
            "florists" or "floral & decor" or "floral" => ["floral", "decor", "flower"],
            _ => Tokenize(category)
        };
    }

    private static bool MatchesCategory(VendorService service, IReadOnlyList<string> keywords)
    {
        if (keywords.Count == 0)
            return true;

        var haystack = string.Join(
            " ",
            service.Category?.Name,
            service.ServiceName,
            service.ServiceDescription,
            service.Vendor?.BusinessDescription)
            .ToLowerInvariant();

        return keywords.Any(k => haystack.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static decimal ComputeStyleFitness(
        IReadOnlyList<string> styleTokens,
        IReadOnlyList<string> venueTokens,
        string? businessDescription,
        string? serviceDescription,
        string serviceName)
    {
        var haystack = string.Join(
            " ",
            businessDescription,
            serviceDescription,
            serviceName)
            .ToLowerInvariant();

        if (styleTokens.Count == 0 && venueTokens.Count == 0)
            return 0.65m;

        var hits = styleTokens.Count(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase))
            + venueTokens.Count(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
        var tokens = Math.Max(1, styleTokens.Count + venueTokens.Count);
        return Math.Min(1m, 0.45m + (hits / (decimal)tokens) * 0.55m);
    }

    private static string BuildReason(
        string categoryName,
        decimal basePrice,
        decimal remainingBudget,
        decimal averageRating,
        decimal styleFitness,
        string? city)
    {
        var parts = new List<string> { $"Strong {categoryName} fit" };

        if (remainingBudget > 0)
        {
            parts.Add(basePrice <= remainingBudget
                ? "within remaining event budget"
                : "slightly above remaining budget — review pricing");
        }

        if (averageRating > 0)
            parts.Add($"{averageRating:0.#}★ rating");

        if (styleFitness >= 0.75m)
            parts.Add("matches couple style/venue notes");

        if (!string.IsNullOrWhiteSpace(city))
            parts.Add($"based in {city}");

        return string.Join("; ", parts) + ".";
    }

    private static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text
            .Split([' ', ',', ';', '/', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
