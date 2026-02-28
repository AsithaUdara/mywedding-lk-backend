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

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Controller for managing wedding events, including creation, retrieval, budget setting, and style preferences.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IMediator _mediator;

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
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            var userId = GetUserId();
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

        /// <summary>
        /// Retrieves the details of a specific wedding event by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the wedding event.</param>
        /// <returns>The wedding event details if found.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var query = new GetEventByIdQuery { EventId = id };
            var result = await _mediator.Send(query);

            return result is not null ? Ok(result) : NotFound();
        }

        /// <summary>
        /// Retrieves all wedding events where the current user is an owner or organizer.
        /// </summary>
        /// <returns>A list of wedding events.</returns>
        [HttpGet]
        public async Task<IActionResult> GetEventsForCurrentUser()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

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
        [HttpPut("{eventId:guid}/budget")]
        public async Task<IActionResult> SetTotalBudget(Guid eventId, [FromBody] SetTotalBudgetRequest request)
        {
            var userId = GetUserId();
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
        /// Sets or updates the wedding style preferences (e.g., Theme, Vibe) for an event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the wedding event.</param>
        /// <param name="preferences">A dictionary of key-value pairs representing style preferences.</param>
        /// <returns>NoContent if successful.</returns>
        [HttpPut("{eventId:guid}/preferences")]
        public async Task<IActionResult> SetStylePreferences(Guid eventId, [FromBody] Dictionary<string, string> preferences)
        {
            var userId = GetUserId();
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

            return NoContent();
        }
    }

    /// <summary>Request DTO for creating a new wedding event.</summary>
    public record CreateEventRequest(string EventName, DateTime EventDate);

    /// <summary>Request DTO for setting the total budget.</summary>
    public record SetTotalBudgetRequest(decimal TotalBudget);
}
