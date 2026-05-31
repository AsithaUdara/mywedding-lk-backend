using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.API.Features.Planner;
using MyWedding.Events.Application.Features.Events.Commands.UpdateEventLifecycleStage;
using MyWedding.Infrastructure.Persistence;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlannerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly IMediator _mediator;

    public PlannerController(
        ApplicationDbContext db,
        IFirebaseAuthService firebaseAuthService,
        IMediator mediator)
    {
        _db = db;
        _firebaseAuthService = firebaseAuthService;
        _mediator = mediator;
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] PlannerSignupRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return BadRequest(new { message = "User is not synced yet. Please login again." });

        var existing = await _db.WeddingPlanners.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is null)
        {
            existing = new WeddingPlanner
            {
                UserId = userId,
                BusinessName = request.BusinessName.Trim(),
                BusinessDescription = request.BusinessDescription?.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                City = request.City?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.WeddingPlanners.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.BusinessName = request.BusinessName.Trim();
            existing.BusinessDescription = request.BusinessDescription?.Trim();
            existing.ContactPhone = request.ContactPhone?.Trim();
            existing.City = request.City?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }

        var hasSub = await _db.PlannerSubscriptions.AnyAsync(s => s.PlannerId == userId && s.Status == SubscriptionStatus.Active, cancellationToken);
        if (!hasSub)
        {
            await _db.PlannerSubscriptions.AddAsync(new PlannerSubscription
            {
                Id = Guid.NewGuid(),
                PlannerId = userId,
                Tier = SubscriptionPlanTier.Free,
                Status = SubscriptionStatus.Active,
                MonthlyFee = 0,
                MaxConcurrentEvents = 1,
                StartsAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _firebaseAuthService.SetUserRoleAsync(userId, "planner");

        return Ok(new { message = "Planner profile created.", plannerId = userId });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var planner = await _db.WeddingPlanners
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (planner is null)
            return NotFound(new { message = "Planner profile not found." });

        var activeSub = await _db.PlannerSubscriptions
            .Where(s => s.PlannerId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var events = await _db.PlannerClientEvents
            .AsNoTracking()
            .Where(e => e.PlannerId == userId)
            .Include(e => e.WeddingEvent)
            .Include(e => e.ClientUser)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new PlannerClientEventSummary(
                e.Id,
                e.EventId,
                e.WeddingEvent != null ? e.WeddingEvent.EventName : "Untitled Event",
                e.WeddingEvent != null ? e.WeddingEvent.EventDate : DateTime.UtcNow,
                e.ClientUserId,
                e.ClientUser != null ? e.ClientUser.Email : string.Empty,
                e.Status.ToString()
            ))
            .ToListAsync(cancellationToken);

        return Ok(new PlannerDashboardResponse(
            planner.UserId,
            $"{planner.User?.FirstName} {planner.User?.LastName}".Trim(),
            planner.BusinessName,
            planner.BusinessDescription,
            planner.ContactPhone,
            planner.City,
            activeSub?.Tier.ToString() ?? SubscriptionPlanTier.Free.ToString(),
            activeSub?.MaxConcurrentEvents ?? 1,
            events,
            planner.AgencyLogoUrl
        ));
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var planner = await _db.WeddingPlanners
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (planner is null)
            return NotFound(new { message = "Planner profile not found." });

        var activeSub = await _db.PlannerSubscriptions
            .Where(s => s.PlannerId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var plannerLinks = await _db.PlannerClientEvents
            .AsNoTracking()
            .Where(e => e.PlannerId == userId)
            .Include(e => e.WeddingEvent)
            .Include(e => e.ClientUser)
            .OrderBy(e => e.WeddingEvent!.EventDate)
            .ToListAsync(cancellationToken);

        var eventIds = plannerLinks.Select(e => e.EventId).Distinct().ToList();

        var pendingBookings = await _db.VendorBookings.CountAsync(
            b => eventIds.Contains(b.EventId) &&
                (b.Status == BookingStatus.Requested || b.Status == BookingStatus.AwaitingPayment),
            cancellationToken);

        var confirmedBookings = await _db.VendorBookings.CountAsync(
            b => eventIds.Contains(b.EventId) && b.Status == BookingStatus.Confirmed,
            cancellationToken);

        var upcomingEvents = plannerLinks
            .Where(e => e.WeddingEvent != null && e.WeddingEvent.EventDate >= DateTime.UtcNow.Date)
            .Take(5)
            .Select(e => new PlannerUpcomingEventDto(
                e.EventId,
                e.WeddingEvent!.EventName,
                e.WeddingEvent.EventDate,
                e.ClientUser?.Email ?? string.Empty,
                e.Status.ToString(),
                e.WeddingEvent.TotalBudget
            ))
            .ToList();

        return Ok(new PlannerOverviewResponse(
            planner.UserId,
            $"{planner.User?.FirstName} {planner.User?.LastName}".Trim(),
            planner.BusinessName,
            planner.BusinessDescription,
            planner.City,
            activeSub?.Tier.ToString() ?? SubscriptionPlanTier.Free.ToString(),
            activeSub?.MaxConcurrentEvents ?? 1,
            plannerLinks.Count(e => e.Status == PlannerClientEventStatus.Active),
            pendingBookings,
            confirmedBookings,
            upcomingEvents,
            activeSub?.MonthlyFee ?? 0,
            activeSub?.EndsAt,
            activeSub?.StartsAt
        ));
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients(CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var clients = await _db.PlannerClientEvents
            .AsNoTracking()
            .Where(e => e.PlannerId == plannerId)
            .Include(e => e.ClientUser)
            .GroupBy(e => new { e.ClientUserId, Email = e.ClientUser != null ? e.ClientUser.Email : string.Empty })
            .Select(g => new PlannerClientDto(
                g.Key.ClientUserId,
                g.Key.Email,
                g.Count(),
                g.Count(x => x.Status == PlannerClientEventStatus.Active),
                g.Max(x => x.CreatedAt)
            ))
            .OrderByDescending(c => c.TotalEvents)
            .ToListAsync(cancellationToken);

        return Ok(clients);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetPlannerEvents([FromQuery] PlannerClientEventStatus? status, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var linksQuery = _db.PlannerClientEvents
            .AsNoTracking()
            .Where(e => e.PlannerId == plannerId)
            .Include(e => e.WeddingEvent)
            .Include(e => e.ClientUser)
            .AsQueryable();

        if (status.HasValue)
            linksQuery = linksQuery.Where(e => e.Status == status.Value);

        var links = await linksQuery
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var eventIds = links.Select(e => e.EventId).Distinct().ToList();

        var weddingEventsById = await _db.WeddingEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(w => eventIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var expenseByEvent = await _db.Expenses
            .AsNoTracking()
            .Where(e => eventIds.Contains(e.EventId))
            .GroupBy(e => e.EventId)
            .Select(g => new { EventId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(k => k.EventId, v => v.Total, cancellationToken);

        var bookingByEvent = await _db.VendorBookings
            .AsNoTracking()
            .Where(b => eventIds.Contains(b.EventId))
            .GroupBy(b => b.EventId)
            .Select(g => new
            {
                EventId = g.Key,
                Requested = g.Count(x => x.Status == BookingStatus.Requested || x.Status == BookingStatus.AwaitingPayment),
                Confirmed = g.Count(x => x.Status == BookingStatus.Confirmed),
                Completed = g.Count(x => x.Status == BookingStatus.Completed)
            })
            .ToDictionaryAsync(k => k.EventId, v => new { v.Requested, v.Confirmed, v.Completed }, cancellationToken);

        var result = links.Select(e =>
        {
            weddingEventsById.TryGetValue(e.EventId, out var weddingEvent);
            var spent = expenseByEvent.TryGetValue(e.EventId, out var total) ? total : 0m;
            var booking = bookingByEvent.TryGetValue(e.EventId, out var b)
                ? b
                : new { Requested = 0, Confirmed = 0, Completed = 0 };

            return new PlannerEventListItemDto(
                e.Id,
                e.EventId,
                weddingEvent?.EventName ?? e.WeddingEvent?.EventName ?? "Untitled Event",
                weddingEvent?.EventDate ?? e.WeddingEvent?.EventDate ?? DateTime.UtcNow,
                e.ClientUserId,
                e.ClientUser?.Email ?? string.Empty,
                e.Status.ToString(),
                weddingEvent?.TotalBudget ?? e.WeddingEvent?.TotalBudget ?? 0m,
                spent,
                booking.Requested,
                booking.Confirmed,
                booking.Completed,
                (weddingEvent ?? e.WeddingEvent)?.EventLifecycleStage.ToString() ?? EventLifecycleStage.Lead.ToString()
            );
        });

        return Ok(result);
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetPlannerBookings(CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var eventIds = await _db.PlannerClientEvents
            .AsNoTracking()
            .Where(e => e.PlannerId == plannerId)
            .Select(e => e.EventId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (eventIds.Count == 0)
            return Ok(Array.Empty<PlannerBookingListItemDto>());

        var bookings = await _db.VendorBookings
            .AsNoTracking()
            .Where(b => eventIds.Contains(b.EventId))
            .Include(b => b.VendorService!)
                .ThenInclude(s => s.Vendor)
            .Include(b => b.WeddingEvent)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(b => b.Id).ToList();
        var paymentTransactions = await _db.BookingPaymentTransactions
            .AsNoTracking()
            .Where(t => bookingIds.Contains(t.BookingId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var paymentByBooking = paymentTransactions
            .GroupBy(t => t.BookingId)
            .ToDictionary(g => g.Key, g => g.First().Status.ToString());

        var items = bookings.Select(b => new PlannerBookingListItemDto(
            b.Id,
            b.EventId,
            b.WeddingEvent?.EventName ?? "Untitled Event",
            b.VendorService?.ServiceName ?? "Vendor service",
            b.VendorService?.Vendor?.BusinessName ?? "Vendor",
            b.Status.ToString(),
            paymentByBooking.TryGetValue(b.Id, out var paymentStatus) ? paymentStatus : "None",
            b.FinalAmount,
            b.CreatedAt
        ));

        return Ok(items);
    }

    [HttpPatch("events/{eventId:guid}/stage")]
    public async Task<IActionResult> UpdateEventStage(Guid eventId, [FromBody] UpdateEventLifecycleStageRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EventLifecycleStage>(request.Stage, true, out var stage))
        {
            return BadRequest(new { message = "Invalid lifecycle stage." });
        }

        await _mediator.Send(new UpdateEventLifecycleStageCommand
        {
            EventId = eventId,
            NewStage = stage
        }, cancellationToken);

        return Ok(new { eventId, stage = stage.ToString() });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreatePlannerEvent([FromBody] CreatePlannerEventRequest request, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var result = await _mediator.Send(new CreatePlannerEventCommand
        {
            PlannerId = plannerId,
            EventName = request.EventName,
            EventDate = request.EventDate,
            TotalBudget = request.TotalBudget,
            ClientUserId = request.ClientUserId,
            ClientEmail = request.ClientEmail
        }, cancellationToken);

        return Ok(new
        {
            eventId = result.EventId,
            plannerClientEventId = result.PlannerClientEventId,
            tasksGenerated = result.TasksGenerated
        });
    }

    [HttpPost("events/{eventId:guid}/assign-client")]
    public async Task<IActionResult> AssignClient(Guid eventId, [FromBody] AssignPlannerClientRequest request, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var eventLink = await _db.PlannerClientEvents.FirstOrDefaultAsync(e => e.PlannerId == plannerId && e.EventId == eventId, cancellationToken);
        if (eventLink is null)
            return NotFound(new { message = "Planner event mapping not found." });

        var clientUser = await ResolveClientUserAsync(request.ClientUserId, request.ClientEmail, cancellationToken);
        if (clientUser is null)
            return BadRequest(new { message = "Client user not found." });

        eventLink.ClientUserId = clientUser.Id;
        eventLink.UpdatedAt = DateTime.UtcNow;

        var existingOrganizer = await _db.EventOrganizers.FirstOrDefaultAsync(o => o.EventId == eventId && o.UserId == clientUser.Id, cancellationToken);
        if (existingOrganizer is null)
        {
            await _db.EventOrganizers.AddAsync(new EventOrganizer
            {
                EventId = eventId,
                UserId = clientUser.Id,
                Role = OrganizerRole.Bride,
                PermissionLevel = PermissionLevel.Editor,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Client assigned to planner event." });
    }

    [HttpGet("events/{eventId:guid}/access-check")]
    public async Task<IActionResult> CheckPlannerEventAccess(Guid eventId, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var hasAccess = await _db.PlannerClientEvents
            .AnyAsync(e => e.PlannerId == plannerId && e.EventId == eventId, cancellationToken);

        return Ok(new { eventId, hasAccess });
    }

    [HttpPost("subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdatePlannerSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var plannerExists = await _db.WeddingPlanners.AnyAsync(p => p.UserId == plannerId, cancellationToken);
        if (!plannerExists)
            return NotFound(new { message = "Planner profile not found." });

        var activeSubs = await _db.PlannerSubscriptions
            .Where(s => s.PlannerId == plannerId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sub in activeSubs)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.EndsAt = DateTime.UtcNow;
        }

        var maxConcurrentEvents = request.Tier == SubscriptionPlanTier.PlannerPro ? 10 : 1;
        var monthlyFee = request.Tier == SubscriptionPlanTier.PlannerPro ? request.MonthlyFee : 0;

        var periodStart = DateTime.UtcNow;
        await _db.PlannerSubscriptions.AddAsync(new PlannerSubscription
        {
            Id = Guid.NewGuid(),
            PlannerId = plannerId,
            Tier = request.Tier,
            Status = SubscriptionStatus.Active,
            MonthlyFee = monthlyFee,
            MaxConcurrentEvents = maxConcurrentEvents,
            StartsAt = periodStart,
            EndsAt = request.Tier == SubscriptionPlanTier.PlannerPro
                ? periodStart.AddMonths(1)
                : null,
            CreatedAt = periodStart
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Planner subscription updated.", maxConcurrentEvents });
    }

    [HttpGet("billing-profile")]
    public async Task<IActionResult> GetBillingProfile(CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var profile = await _db.PlannerBillingProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlannerId == plannerId, cancellationToken);

        if (profile is null)
            return Ok(new { hasPaymentMethod = false });

        return Ok(new
        {
            hasPaymentMethod = !string.IsNullOrEmpty(profile.Last4),
            cardholderName = profile.CardholderName,
            cardBrand = profile.CardBrand,
            last4 = profile.Last4,
            expiryMonth = profile.ExpiryMonth,
            expiryYear = profile.ExpiryYear,
            updatedAt = profile.UpdatedAt,
        });
    }

    [HttpPut("billing-profile")]
    public async Task<IActionResult> SaveBillingProfile(
        [FromBody] PlannerBillingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Last4) || request.Last4.Length != 4 || !request.Last4.All(char.IsDigit))
            return BadRequest(new { message = "Only the last 4 digits are stored. Enter a valid last-4." });

        var profile = await _db.PlannerBillingProfiles
            .FirstOrDefaultAsync(p => p.PlannerId == plannerId, cancellationToken);

        if (profile is null)
        {
            profile = new PlannerBillingProfile { PlannerId = plannerId };
            await _db.PlannerBillingProfiles.AddAsync(profile, cancellationToken);
        }

        profile.CardholderName = request.CardholderName?.Trim();
        profile.CardBrand = request.CardBrand?.Trim();
        profile.Last4 = request.Last4;
        profile.ExpiryMonth = request.ExpiryMonth;
        profile.ExpiryYear = request.ExpiryYear;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Payment method saved (masked). Full card numbers are never stored." });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePlannerProfileRequest request, CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var planner = await _db.WeddingPlanners.FirstOrDefaultAsync(p => p.UserId == plannerId, cancellationToken);
        if (planner is null)
            return NotFound(new { message = "Planner profile not found." });

        planner.BusinessName = request.BusinessName.Trim();
        planner.BusinessDescription = request.BusinessDescription?.Trim();
        planner.ContactPhone = request.ContactPhone?.Trim();
        planner.City = request.City?.Trim();
        planner.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Planner profile updated." });
    }

    [HttpPut("profile/agency-logo")]
    public async Task<IActionResult> UpdateAgencyLogo(
        [FromBody] UpdatePlannerAgencyLogoRequest request,
        CancellationToken cancellationToken)
    {
        var plannerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.AgencyLogoUrl))
            return BadRequest(new { message = "Agency logo URL is required." });

        var activeSub = await _db.PlannerSubscriptions
            .Where(s => s.PlannerId == plannerId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSub?.Tier != SubscriptionPlanTier.PlannerPro)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Upgrade to Planner Pro to unlock white-labeling."
            });
        }

        var planner = await _db.WeddingPlanners.FirstOrDefaultAsync(p => p.UserId == plannerId, cancellationToken);
        if (planner is null)
            return NotFound(new { message = "Planner profile not found." });

        planner.AgencyLogoUrl = request.AgencyLogoUrl.Trim();
        planner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Agency logo updated.", agencyLogoUrl = planner.AgencyLogoUrl });
    }

    private async Task<User?> ResolveClientUserAsync(string? clientUserId, string? clientEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(clientUserId))
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == clientUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(clientEmail))
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == clientEmail, cancellationToken);

        return null;
    }
}

public record PlannerSignupRequest(string BusinessName, string? BusinessDescription, string? ContactPhone, string? City);
public record CreatePlannerEventRequest(string EventName, DateTime EventDate, decimal TotalBudget, string? ClientUserId, string? ClientEmail);
public record AssignPlannerClientRequest(string? ClientUserId, string? ClientEmail);
public record UpdatePlannerSubscriptionRequest(SubscriptionPlanTier Tier, decimal MonthlyFee);
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
    string EventLifecycleStage
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
