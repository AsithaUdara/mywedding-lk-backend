using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/planner/ai")]
[Authorize]
public class PlannerAiController : ControllerBase
{
    private readonly IAiCopilotService _aiCopilotService;
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _db;
    private readonly IWeddingEventRepository _eventRepository;

    public PlannerAiController(
        IAiCopilotService aiCopilotService,
        IMediator mediator,
        ApplicationDbContext db,
        IWeddingEventRepository eventRepository)
    {
        _aiCopilotService = aiCopilotService;
        _mediator = mediator;
        _db = db;
        _eventRepository = eventRepository;
    }

    [HttpPost("suggest-tasks")]
    public async Task<IActionResult> SuggestTasks(
        [FromBody] SuggestTasksRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _aiCopilotService.SummarizeMeetingToTasksAsync(
            new MeetingSummaryRequest(request.EventId, request.EventName, request.Notes),
            cancellationToken);

        return Ok(new
        {
            result.ExecutiveSummary,
            proposedTasks = result.ProposedTasks,
            result.IsSimulated
        });
    }

    [HttpPost("suggest-vendors")]
    public async Task<IActionResult> SuggestVendors(
        [FromBody] SuggestVendorsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var weddingEvent = await _db.WeddingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
        if (weddingEvent is null)
            return NotFound();

        var canManage = weddingEvent.ManagingPlannerId == userId
            || weddingEvent.CreatedById == userId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, userId, cancellationToken);
        if (!canManage)
            return Forbid();

        var recommendations = await BuildVendorSuggestionsAsync(
            weddingEvent,
            request.Category,
            request.TopN <= 0 ? 5 : request.TopN,
            cancellationToken);

        return Ok(recommendations);
    }

    [HttpPost("personalize-checklist-plan")]
    public async Task<IActionResult> PersonalizeChecklistPlan(
        [FromBody] PersonalizeChecklistPlanRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GeneratePersonalizedChecklistPlanCommand
        {
            EventId = request.EventId,
            UserId = userId,
            MeetingNotesOrTranscript = request.MeetingNotesOrTranscript
        });

        return Ok(result);
    }

    private async Task<List<PlannerVendorSuggestionDto>> BuildVendorSuggestionsAsync(
        WeddingEvent weddingEvent,
        string category,
        int topN,
        CancellationToken cancellationToken)
    {
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

        var scored = services
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

        return scored;
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

public record PersonalizeChecklistPlanRequest(Guid EventId, string? MeetingNotesOrTranscript);

public record SuggestTasksRequest(
    Guid EventId,
    string EventName,
    string Notes);

public record SuggestVendorsRequest(
    Guid EventId,
    string Category,
    int TopN = 5);

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
