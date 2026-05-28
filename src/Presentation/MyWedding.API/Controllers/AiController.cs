using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Infrastructure.Persistence;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (request.Message.Length > 2000)
            return BadRequest(new { message = "Message too long." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var hasAccess = await _db.EventOrganizers
            .AnyAsync(o => o.EventId == request.EventId && o.UserId == userId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        var recommendations = await BuildRecommendationsAsync(request.EventId, 3, cancellationToken);
        var quickReply = recommendations.Count == 0
            ? "I could not find matching vendors yet. Add style preferences and budget details first."
            : $"Based on your style and budget, top picks are: {string.Join(", ", recommendations.Select(r => r.BusinessName))}.";

        return Ok(new
        {
            reply = $"{quickReply} You asked: \"{request.Message}\"",
            recommendations
        });
    }

    [HttpPost("recommend-vendors")]
    public async Task<IActionResult> RecommendVendors([FromBody] VendorRecommendationRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var hasAccess = await _db.EventOrganizers
            .AnyAsync(o => o.EventId == request.EventId && o.UserId == userId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        var recommendations = await BuildRecommendationsAsync(request.EventId, request.TopN <= 0 ? 5 : request.TopN, cancellationToken);
        return Ok(recommendations);
    }

    [HttpPost("itinerary/generate")]
    public async Task<IActionResult> GenerateItinerary([FromBody] GenerateItineraryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var hasAccess = await _db.EventOrganizers
            .AnyAsync(o => o.EventId == request.EventId && o.UserId == userId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        var weddingEvent = await _db.WeddingEvents.FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
        if (weddingEvent is null)
            return NotFound();

        var confirmedBookings = await _db.VendorBookings
            .Where(b => b.EventId == request.EventId && b.Status == Domain.Enums.BookingStatus.Confirmed)
            .Include(b => b.VendorService)
            .OrderBy(b => b.ServiceDate)
            .ToListAsync(cancellationToken);

        var itinerary = await _db.EventItineraries
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.EventId == request.EventId, cancellationToken);

        if (itinerary is null)
        {
            itinerary = new EventItinerary
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                IsAiGenerated = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.EventItineraries.AddAsync(itinerary, cancellationToken);
        }
        else
        {
            _db.EventItineraryItems.RemoveRange(itinerary.Items);
            itinerary.Items.Clear();
            itinerary.UpdatedAt = DateTime.UtcNow;
        }

        var eventDateBase = weddingEvent.EventDate.Date;
        var blockStart = eventDateBase.AddHours(8);
        var sortOrder = 1;
        foreach (var booking in confirmedBookings)
        {
            var duration = request.DefaultServiceDurationHours <= 0 ? 2 : request.DefaultServiceDurationHours;
            var item = new EventItineraryItem
            {
                Id = Guid.NewGuid(),
                ItineraryId = itinerary.Id,
                Title = booking.VendorService?.ServiceName ?? "Vendor Service",
                Description = $"Scheduled from confirmed booking {booking.Id}",
                StartsAt = blockStart,
                EndsAt = blockStart.AddHours(duration),
                SortOrder = sortOrder++
            };
            blockStart = item.EndsAt.AddMinutes(30);
            itinerary.Items.Add(item);
        }

        if (itinerary.Items.Count == 0)
        {
            itinerary.Items.Add(new EventItineraryItem
            {
                Id = Guid.NewGuid(),
                ItineraryId = itinerary.Id,
                Title = "Wedding Ceremony",
                Description = "Default timeline placeholder. Confirm vendors to auto-build timeline.",
                StartsAt = eventDateBase.AddHours(9),
                EndsAt = eventDateBase.AddHours(10),
                SortOrder = 1
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { itineraryId = itinerary.Id });
    }

    [HttpGet("itinerary/{eventId:guid}")]
    public async Task<IActionResult> GetItinerary(Guid eventId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var hasAccess = await _db.EventOrganizers.AnyAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        var itinerary = await _db.EventItineraries
            .AsNoTracking()
            .Include(i => i.Items.OrderBy(x => x.SortOrder))
            .FirstOrDefaultAsync(i => i.EventId == eventId, cancellationToken);
        if (itinerary is null)
            return NotFound();

        return Ok(new
        {
            itineraryId = itinerary.Id,
            eventId = itinerary.EventId,
            isAiGenerated = itinerary.IsAiGenerated,
            items = itinerary.Items.Select(i => new
            {
                i.Id,
                i.Title,
                i.Description,
                i.StartsAt,
                i.EndsAt,
                i.SortOrder
            })
        });
    }

    [HttpPut("itinerary/{itineraryId:guid}")]
    public async Task<IActionResult> SaveItinerary(Guid itineraryId, [FromBody] SaveItineraryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var itinerary = await _db.EventItineraries
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == itineraryId, cancellationToken);
        if (itinerary is null)
            return NotFound();

        var hasAccess = await _db.EventOrganizers.AnyAsync(o => o.EventId == itinerary.EventId && o.UserId == userId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        _db.EventItineraryItems.RemoveRange(itinerary.Items);
        itinerary.Items = request.Items.Select((i, index) => new EventItineraryItem
        {
            Id = Guid.NewGuid(),
            ItineraryId = itinerary.Id,
            Title = i.Title,
            Description = i.Description,
            StartsAt = i.StartsAt,
            EndsAt = i.EndsAt,
            SortOrder = index + 1
        }).ToList();

        itinerary.UpdatedAt = DateTime.UtcNow;
        itinerary.IsAiGenerated = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Itinerary saved." });
    }

    private async Task<List<VendorRecommendationDto>> BuildRecommendationsAsync(Guid eventId, int topN, CancellationToken cancellationToken)
    {
        var weddingEvent = await _db.WeddingEvents.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (weddingEvent is null)
            return new List<VendorRecommendationDto>();

        var expenseTotal = await _db.Expenses.Where(e => e.EventId == eventId).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var remainingBudget = weddingEvent.TotalBudget - expenseTotal;

        var vendors = await _db.Vendors
            .AsNoTracking()
            .Include(v => v.Services)
            .Where(v => v.VerificationStatus == Domain.Enums.VerificationStatus.Verified)
            .ToListAsync(cancellationToken);

        return vendors
            .Select(v =>
            {
                var avgPrice = v.Services.Any() ? v.Services.Average(s => s.BasePrice) : 0;
                var priceFitness = avgPrice <= remainingBudget ? 1m : 0.4m;
                var styleFitness = 0.8m;
                var ratingFitness = v.AverageRating <= 0 ? 0.5m : Math.Min(v.AverageRating / 5m, 1m);
                var score = Math.Round((priceFitness * 0.4m) + (styleFitness * 0.3m) + (ratingFitness * 0.3m), 3);

                return new VendorRecommendationDto(
                    v.UserId,
                    v.BusinessName,
                    score,
                    $"Fits budget ({remainingBudget:0.00}) and style profile."
                );
            })
            .OrderByDescending(v => v.Score)
            .Take(topN)
            .ToList();
    }

}

public record AiChatRequest(Guid EventId, string Message);
public record VendorRecommendationRequest(Guid EventId, int TopN = 5);
public record VendorRecommendationDto(string VendorId, string BusinessName, decimal Score, string Reason);
public record GenerateItineraryRequest(Guid EventId, int DefaultServiceDurationHours = 2);
public record SaveItineraryRequest(IReadOnlyCollection<SaveItineraryItem> Items);
public record SaveItineraryItem(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);
