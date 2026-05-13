using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;






using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Manages budget-related operations for wedding events, including expense tracking and budget overview.
/// All endpoints require the caller to be an organizer of the target event.
/// </summary>
[ApiController]
[Authorize]
public class BudgetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEventOrganizerRepository _organizerRepository;

    public BudgetController(IMediator mediator, IEventOrganizerRepository organizerRepository)
    {
        _mediator = mediator;
        _organizerRepository = organizerRepository;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Retrieves the full budget overview for an event, including allocated amounts and spent totals per category.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <returns>Budget overview data if found.</returns>
    [HttpGet("api/events/{eventId:guid}/budget")]
    public async Task<IActionResult> GetBudgetOverview(Guid eventId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var isMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId);
        if (!isMember) return Forbid();

        var query = new GetBudgetOverviewQuery { EventId = eventId };
        var result = await _mediator.Send(query);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Adds a new expense entry to an event's budget.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <param name="request">The expense details.</param>
    /// <returns>201 Created with the new expense ID.</returns>
    [HttpPost("api/events/{eventId:guid}/expenses")]
    public async Task<IActionResult> AddExpense(Guid eventId, [FromBody] AddExpenseRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var isMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId);
        if (!isMember) return Forbid();

        var command = new AddExpenseCommand
        {
            EventId = eventId,
            Title = request.Title,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            BudgetCategoryId = request.BudgetCategoryId,
            UserId = userId
        };

        var expenseId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetBudgetOverview), new { eventId }, new { ExpenseId = expenseId });
    }

    /// <summary>
    /// Retrieves all expenses for a specific wedding event.
    /// Requires the caller to be an organizer of the event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the wedding event.</param>
    /// <returns>A list of expense entries.</returns>
    [HttpGet("api/events/{eventId:guid}/expenses")]
    public async Task<IActionResult> GetExpenses(Guid eventId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Security: only organizers of this event can view its expenses
        var isMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId);
        if (!isMember) return Forbid();

        var query = new GetExpensesByEventIdQuery(eventId);
        var expenses = await _mediator.Send(query);
        return Ok(expenses);
    }

    /// <summary>
    /// Retrieves all available budget categories (e.g., Venue, Photography, Catering).
    /// </summary>
    /// <returns>A list of budget categories.</returns>
    [HttpGet("api/budget-categories")]
    public async Task<IActionResult> GetBudgetCategories()
    {
        var query = new GetBudgetCategoriesQuery();
        var categories = await _mediator.Send(query);
        return Ok(categories);
    }
}
