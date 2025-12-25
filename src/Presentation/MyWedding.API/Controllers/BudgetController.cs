// File: src/Presentation/MyWedding.API/Controllers/BudgetController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.BudgetCategories.Queries.GetBudgetCategories;
using MyWedding.Application.Features.Events.Queries.GetBudgetOverview;
using MyWedding.Application.Features.Events.Queries.GetExpensesByEventId;
using MyWedding.Application.Features.Expenses.Commands.AddExpense;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Authorize] // All endpoints in this controller require authentication
public class BudgetController : ControllerBase
{
    private readonly IMediator _mediator;

    public BudgetController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/events/{eventId}/budget
    [HttpGet("api/events/{eventId:guid}/budget")]
    public async Task<IActionResult> GetBudgetOverview(Guid eventId)
    {
        // TODO: Add security check to ensure user is an organizer of this event
        var query = new GetBudgetOverviewQuery { EventId = eventId };
        var result = await _mediator.Send(query);
        return result is not null ? Ok(result) : NotFound();
    }

    // POST /api/events/{eventId}/expenses
    [HttpPost("api/events/{eventId:guid}/expenses")]
    public async Task<IActionResult> AddExpense(Guid eventId, [FromBody] AddExpenseRequest request)
    {
        // TODO: Add security check to ensure user has 'Editor' or 'Owner' permissions
        var command = new AddExpenseCommand
        {
            EventId = eventId,
            Title = request.Title,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            BudgetCategoryId = request.BudgetCategoryId
        };

        var expenseId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetBudgetOverview), new { eventId = eventId }, new { ExpenseId = expenseId });
    }

    // GET /api/budget-categories
    [HttpGet("api/budget-categories")]
    public async Task<IActionResult> GetBudgetCategories()
    {
        var query = new GetBudgetCategoriesQuery();
        var categories = await _mediator.Send(query);
        return Ok(categories);
    }

    // GET /api/events/{eventId}/expenses
    [HttpGet("api/events/{eventId:guid}/expenses")]
    public async Task<IActionResult> GetExpenses(Guid eventId)
    {
        // TODO: Add security check to ensure user is an organizer of this event
        var query = new GetExpensesByEventIdQuery(eventId);
        var expenses = await _mediator.Send(query);
        return Ok(expenses);
    }
}

// --- DTOs for the request bodies ---
public record AddExpenseRequest(string Title, decimal Amount, DateTime ExpenseDate, Guid BudgetCategoryId);
