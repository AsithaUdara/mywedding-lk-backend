// File: src/Presentation/MyWedding.API/Controllers/EventsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Events.Commands.CreateEvent;
using MyWedding.Application.Features.Events.Commands.SetEventPreferences;
using MyWedding.Application.Features.Events.Commands.SetTotalBudget;
using MyWedding.Application.Features.Events.Queries.GetEventById;
using MyWedding.Application.Features.Events.Queries.GetEventsByUserId;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize] // This entire controller is protected and requires a valid token
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var query = new GetEventByIdQuery { EventId = id };
        var result = await _mediator.Send(query);

        // Ensure the current user owns this event (security check)
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (result != null && result.CreatedById != currentUserId)
        {
            return Forbid(); // User is trying to access an event that is not theirs
        }

        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetEventsForCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var query = new GetEventsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    // --- NEW ENDPOINT ---
    [HttpPut("{eventId:guid}/budget")]
    public async Task<IActionResult> SetTotalBudget(Guid eventId, [FromBody] SetTotalBudgetRequest request)
    {
        // TODO: Add security check to ensure user has 'Editor' or 'Owner' permissions
        var command = new SetTotalBudgetCommand
        {
            EventId = eventId,
            TotalBudget = request.TotalBudget
        };

        await _mediator.Send(command);

        return NoContent(); // 204 No Content is the standard response for a successful PUT
    }

    // --- STYLE PREFERENCES ENDPOINT ---
    [HttpPut("{eventId:guid}/preferences")]
    public async Task<IActionResult> SetStylePreferences(Guid eventId, [FromBody] Dictionary<string, string> preferences)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new SetEventPreferencesCommand
        {
            EventId = eventId,
            UserId = userId,
            Preferences = preferences
        };

        await _mediator.Send(command);

        return NoContent(); // 204 No Content is the standard success response for a PUT
    }
}

// This is a simple DTO (Data Transfer Object) for the request body
public record CreateEventRequest(string EventName, DateTime EventDate);

// --- NEW DTO FOR THE NEW ENDPOINT ---
public record SetTotalBudgetRequest(decimal TotalBudget);
