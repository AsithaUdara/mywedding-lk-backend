using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Events.Application.Features.EventBrief.GetEventBrief;
using MyWedding.Events.Application.Features.EventBrief.UpdateEventBrief;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Wedding event brief: guest counts, style preferences, and planning notes.
/// </summary>
[ApiController]
[Authorize]
public class EventBriefController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBriefController"/> class.
        /// </summary>
    public EventBriefController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Retrieves the planning brief for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <response code="200">Event brief returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("api/events/{eventId:guid}/brief")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Updates the planning brief for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">Guest counts, style, venue preferences, and notes.</param>
    /// <response code="200">Brief updated.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("api/events/{eventId:guid}/brief")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

/// <summary>Payload for updating an event planning brief.</summary>
public record UpdateEventBriefRequest(
    int? EstimatedGuestCount,
    int? GuestCountMax,
    string? WeddingStyle,
    string? VenuePreference,
    string? MustHavesNotes,
    string? ServicesAlreadyBooked,
    string? CulturalOrReligiousNotes,
    bool MarkBriefComplete = false);
