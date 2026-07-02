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
        /// <summary>
        /// Initializes a new instance of the <see cref="BudgetController"/> class.
        /// </summary>
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
    /// <response code="200">Budget overview returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not an event organizer.</response>
    /// <response code="404">Event or budget not found.</response>
    [HttpGet("api/events/{eventId:guid}/budget")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// <response code="201">Expense created.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to add expenses.</response>
    [HttpPost("api/events/{eventId:guid}/expenses")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddExpense(Guid eventId, [FromBody] AddExpenseRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId);
        if (organizer is null) return Forbid();

        if (organizer.PermissionLevel == PermissionLevel.Viewer)
            throw new ForbiddenAccessException("You do not have permission to add expenses for this event.");

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
    /// <response code="200">Expenses returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is not an event organizer.</response>
    [HttpGet("api/events/{eventId:guid}/expenses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// <response code="200">Categories returned.</response>
    [HttpGet("api/budget-categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetCategories()
    {
        var query = new GetBudgetCategoriesQuery();
        var categories = await _mediator.Send(query);
        return Ok(categories);
    }
}
