using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Events.Application.Features.Events.Commands.UpdateEventLifecycleStage;
using MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;
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
            events
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
            upcomingEvents
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

        var planner = await _db.WeddingPlanners.FirstOrDefaultAsync(p => p.UserId == plannerId, cancellationToken);
        if (planner is null)
            return Forbid();

        var activeSub = await _db.PlannerSubscriptions
            .Where(s => s.PlannerId == plannerId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var maxEvents = activeSub?.MaxConcurrentEvents ?? 1;
        var activeCount = await _db.PlannerClientEvents.CountAsync(
            e => e.PlannerId == plannerId && e.Status == PlannerClientEventStatus.Active,
            cancellationToken);
        if (activeCount >= maxEvents)
        {
            return BadRequest(new
            {
                message = "Planner event limit reached for current subscription.",
                maxConcurrentEvents = maxEvents
            });
        }

        var clientUser = await ResolveClientUserAsync(request.ClientUserId, request.ClientEmail, cancellationToken);
        if (clientUser is null)
        {
            return BadRequest(new { message = "Client user not found. Provide a valid clientUserId or synced client email." });
        }

        var weddingEvent = new WeddingEvent
        {
            Id = Guid.NewGuid(),
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate,
            TotalBudget = request.TotalBudget,
            ManagingPlannerId = plannerId,
            EventLifecycleStage = EventLifecycleStage.Lead,
            CreatedById = plannerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var plannerOrganizer = new EventOrganizer
        {
            EventId = weddingEvent.Id,
            UserId = plannerId,
            Role = OrganizerRole.Planner,
            PermissionLevel = PermissionLevel.Owner,
            JoinedAt = DateTime.UtcNow
        };

        var clientOrganizer = new EventOrganizer
        {
            EventId = weddingEvent.Id,
            UserId = clientUser.Id,
            Role = OrganizerRole.Bride,
            PermissionLevel = PermissionLevel.Editor,
            JoinedAt = DateTime.UtcNow
        };

        var plannerClientEvent = new PlannerClientEvent
        {
            Id = Guid.NewGuid(),
            PlannerId = plannerId,
            EventId = weddingEvent.Id,
            ClientUserId = clientUser.Id,
            Status = PlannerClientEventStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.WeddingEvents.AddAsync(weddingEvent, cancellationToken);
        await _db.EventOrganizers.AddRangeAsync(plannerOrganizer, clientOrganizer);
        await _db.PlannerClientEvents.AddAsync(plannerClientEvent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var tasksGenerated = await _mediator.Send(new GenerateTaskTemplateCommand
        {
            EventId = weddingEvent.Id,
            UserId = plannerId
        }, cancellationToken);

        return Ok(new
        {
            eventId = weddingEvent.Id,
            plannerClientEventId = plannerClientEvent.Id,
            tasksGenerated
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

        await _db.PlannerSubscriptions.AddAsync(new PlannerSubscription
        {
            Id = Guid.NewGuid(),
            PlannerId = plannerId,
            Tier = request.Tier,
            Status = SubscriptionStatus.Active,
            MonthlyFee = monthlyFee,
            MaxConcurrentEvents = maxConcurrentEvents,
            StartsAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Planner subscription updated.", maxConcurrentEvents });
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
public record PlannerClientEventSummary(Guid PlannerClientEventId, Guid EventId, string EventName, DateTime EventDate, string ClientUserId, string ClientEmail, string Status);
public record PlannerUpcomingEventDto(Guid EventId, string EventName, DateTime EventDate, string ClientEmail, string Status, decimal TotalBudget);
public record PlannerClientDto(string ClientUserId, string ClientEmail, int TotalEvents, int ActiveEvents, DateTime LastActivityAt);
public record UpdateEventLifecycleStageRequest(string Stage);
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
    IReadOnlyCollection<PlannerUpcomingEventDto> UpcomingEvents
);
public record PlannerDashboardResponse(
    string PlannerId,
    string PlannerName,
    string BusinessName,
    string? BusinessDescription,
    string? ContactPhone,
    string? City,
    string ActivePlanTier,
    int MaxConcurrentEvents,
    IReadOnlyCollection<PlannerClientEventSummary> Events
);
