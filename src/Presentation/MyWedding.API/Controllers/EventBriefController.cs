using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Events.Application.Features.EventBrief.GetEventBrief;
using MyWedding.Events.Application.Features.EventBrief.UpdateEventBrief;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Authorize]
public class EventBriefController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventBriefController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("api/events/{eventId:guid}/brief")]
    public async Task<IActionResult> GetBrief(Guid eventId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetEventBriefQuery { EventId = eventId, UserId = userId });
        return Ok(result);
    }

    [HttpPut("api/events/{eventId:guid}/brief")]
    public async Task<IActionResult> UpdateBrief(Guid eventId, [FromBody] UpdateEventBriefRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new UpdateEventBriefCommand
        {
            EventId = eventId,
            UserId = userId,
            EstimatedGuestCount = request.EstimatedGuestCount,
            GuestCountMax = request.GuestCountMax,
            WeddingStyle = request.WeddingStyle,
            VenuePreference = request.VenuePreference,
            MustHavesNotes = request.MustHavesNotes,
            ServicesAlreadyBooked = request.ServicesAlreadyBooked,
            CulturalOrReligiousNotes = request.CulturalOrReligiousNotes,
            MarkBriefComplete = request.MarkBriefComplete
        });

        return Ok(result);
    }
}

public record UpdateEventBriefRequest(
    int? EstimatedGuestCount,
    int? GuestCountMax,
    string? WeddingStyle,
    string? VenuePreference,
    string? MustHavesNotes,
    string? ServicesAlreadyBooked,
    string? CulturalOrReligiousNotes,
    bool MarkBriefComplete = false);
