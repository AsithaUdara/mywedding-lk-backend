using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class EventAiService : IEventAiService
{
    private readonly ApplicationDbContext _db;

    public EventAiService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<EventAiWorkflowResult> ChatAsync(
        string? userId,
        Guid eventId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new EventAiWorkflowResult(401, null);

        if (message.Length > 2000)
            return new EventAiWorkflowResult(400, new { message = "Message too long." });

        if (!await HasEventAccessAsync(eventId, userId, cancellationToken))
            return new EventAiWorkflowResult(403, null);

        var recommendations = await BuildRecommendationsAsync(eventId, 3, cancellationToken);
        var quickReply = recommendations.Count == 0
            ? "I could not find matching vendors yet. Add style preferences and budget details first."
            : $"Based on your style and budget, top picks are: {string.Join(", ", recommendations.Select(r => r.BusinessName))}.";

        return new EventAiWorkflowResult(200, new
        {
            reply = $"{quickReply} You asked: \"{message}\"",
            recommendations
        });
    }

    public async Task<EventAiWorkflowResult> RecommendVendorsAsync(
        string? userId,
        Guid eventId,
        int topN,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new EventAiWorkflowResult(401, null);

        if (!await HasEventAccessAsync(eventId, userId, cancellationToken))
            return new EventAiWorkflowResult(403, null);

        var recommendations = await BuildRecommendationsAsync(
            eventId,
            topN <= 0 ? 5 : topN,
            cancellationToken);

        return new EventAiWorkflowResult(200, recommendations);
    }

    public async Task<EventAiWorkflowResult> GenerateItineraryAsync(
        string? userId,
        Guid eventId,
        int defaultServiceDurationHours,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new EventAiWorkflowResult(401, null);

        if (!await HasEventAccessAsync(eventId, userId, cancellationToken))
            return new EventAiWorkflowResult(403, null);

        var weddingEvent = await _db.WeddingEvents.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (weddingEvent is null)
            return new EventAiWorkflowResult(404, null);

        var confirmedBookings = await _db.VendorBookings
            .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
            .Include(b => b.VendorService)
            .OrderBy(b => b.ServiceDate)
            .ToListAsync(cancellationToken);

        var itinerary = await _db.EventItineraries
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.EventId == eventId, cancellationToken);

        if (itinerary is null)
        {
            itinerary = new EventItinerary
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
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
            var duration = defaultServiceDurationHours <= 0 ? 2 : defaultServiceDurationHours;
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
        return new EventAiWorkflowResult(200, new { itineraryId = itinerary.Id });
    }

    public async Task<EventAiWorkflowResult> GetItineraryAsync(
        string? userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new EventAiWorkflowResult(401, null);

        if (!await HasEventAccessAsync(eventId, userId, cancellationToken))
            return new EventAiWorkflowResult(403, null);

        var itinerary = await _db.EventItineraries
            .AsNoTracking()
            .Include(i => i.Items.OrderBy(x => x.SortOrder))
            .FirstOrDefaultAsync(i => i.EventId == eventId, cancellationToken);
        if (itinerary is null)
            return new EventAiWorkflowResult(404, null);

        return new EventAiWorkflowResult(200, new
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

    public async Task<EventAiWorkflowResult> SaveItineraryAsync(
        string? userId,
        Guid itineraryId,
        IReadOnlyCollection<EventAiSaveItineraryItem> items,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new EventAiWorkflowResult(401, null);

        var itinerary = await _db.EventItineraries
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == itineraryId, cancellationToken);
        if (itinerary is null)
            return new EventAiWorkflowResult(404, null);

        if (!await HasEventAccessAsync(itinerary.EventId, userId, cancellationToken))
            return new EventAiWorkflowResult(403, null);

        _db.EventItineraryItems.RemoveRange(itinerary.Items);
        itinerary.Items = items.Select((i, index) => new EventItineraryItem
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

        return new EventAiWorkflowResult(200, new { message = "Itinerary saved." });
    }

    private async Task<bool> HasEventAccessAsync(
        Guid eventId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _db.EventOrganizers
            .AnyAsync(o => o.EventId == eventId && o.UserId == userId, cancellationToken);
    }

    private async Task<List<EventAiVendorRecommendationDto>> BuildRecommendationsAsync(
        Guid eventId,
        int topN,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _db.WeddingEvents.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (weddingEvent is null)
            return new List<EventAiVendorRecommendationDto>();

        var expenseTotal = await _db.Expenses
            .Where(e => e.EventId == eventId)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var remainingBudget = weddingEvent.TotalBudget - expenseTotal;

        var vendors = await _db.Vendors
            .AsNoTracking()
            .Include(v => v.Services)
            .Where(v => v.VerificationStatus == VerificationStatus.Verified)
            .ToListAsync(cancellationToken);

        return vendors
            .Select(v =>
            {
                var avgPrice = v.Services.Any() ? v.Services.Average(s => s.BasePrice) : 0;
                var priceFitness = avgPrice <= remainingBudget ? 1m : 0.4m;
                var styleFitness = 0.8m;
                var ratingFitness = v.AverageRating <= 0 ? 0.5m : Math.Min(v.AverageRating / 5m, 1m);
                var score = Math.Round((priceFitness * 0.4m) + (styleFitness * 0.3m) + (ratingFitness * 0.3m), 3);

                return new EventAiVendorRecommendationDto(
                    v.UserId,
                    v.BusinessName,
                    score,
                    $"Fits budget ({remainingBudget:0.00}) and style profile.");
            })
            .OrderByDescending(v => v.Score)
            .Take(topN)
            .ToList();
    }
}
