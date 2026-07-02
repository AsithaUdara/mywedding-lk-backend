using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;







using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Controller for managing wedding events, including creation, retrieval, budget setting, and style preferences.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="EventsController"/> class.
        /// </summary>
    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Creates a new wedding event for the current user.
    /// </summary>
    /// <param name="request">The details of the event to create.</param>
    /// <returns>The ID of the created event.</returns>
    /// <response code="201">Event created successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Only admins may create events directly; couples join via invitation.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // B2B2C: couples join planner-managed events via invitation, not self-service creation.
        if (!User.IsInRole("admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Events are created by your wedding planner. Accept your invitation email to access your celebration."
            });
        }

        var command = new CreateEventCommand
        {
            EventName = request.EventName,
            EventDate = request.EventDate,
            UserId = userId
        };

        var eventId = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetEventById), new { id = eventId }, new { EventId = eventId });
    }

    /// <summary>
    /// Retrieves the details of a specific wedding event by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the wedding event.</param>
    /// <returns>The wedding event details if found.</returns>
    /// <response code="200">Event returned successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">Event not found or access denied.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetEventByIdQuery { EventId = id, UserId = userId };
        var result = await _mediator.Send(query);

        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Retrieves all wedding events where the current user is an owner or organizer.
    /// </summary>
    /// <returns>A list of wedding events.</returns>
    /// <response code="200">Events returned successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEventsForCurrentUser()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetEventsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Sets or updates the total budget for a specific wedding event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <param name="request">The new budget amount.</param>
    /// <returns>NoContent if successful.</returns>
    /// <response code="204">Budget updated successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("{eventId:guid}/budget")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetTotalBudget(Guid eventId, [FromBody] SetTotalBudgetRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new SetTotalBudgetCommand
        {
            EventId = eventId,
            TotalBudget = request.TotalBudget,
            UserId = userId
        };

        await _mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Retrieves the list of invitations sent for a specific wedding event, including their current status.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <returns>A list of invitations and their statuses.</returns>
    /// <response code="200">Invitations returned successfully.</response>
    [HttpGet("{eventId:guid}/invitations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitations(Guid eventId)
    {
        var query = new GetEventInvitationsQuery { EventId = eventId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}
