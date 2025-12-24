// File: src/Presentation/MyWedding.API/Controllers/EventsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Events.Commands.CreateEvent;
using System;
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

        return CreatedAtAction(nameof(CreateEvent), new { id = eventId }, new { EventId = eventId });
    }
}

// This is a simple DTO (Data Transfer Object) for the request body
public record CreateEventRequest(string EventName, DateTime EventDate);
