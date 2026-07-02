using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Planner.Application.Features.Planner.TaskTemplates;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Planner task templates: save, apply, and reuse task sets across events.
/// </summary>
[ApiController]
[Route("api/planner/task-templates")]
[Authorize]
public class PlannerTaskTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="PlannerTaskTemplatesController"/> class.
        /// </summary>
    public PlannerTaskTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetPlannerId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Lists all task templates owned by the authenticated planner.
    /// </summary>
    /// <response code="200">Templates returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var plannerId = GetPlannerId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var templates = await _mediator.Send(
            new GetPlannerTaskTemplatesQuery { PlannerId = plannerId },
            cancellationToken);

        return Ok(templates);
    }

    /// <summary>
    /// Saves an event's current tasks as a reusable template.
    /// </summary>
    /// <param name="eventId">The source event identifier.</param>
    /// <param name="request">Template name and description.</param>
    /// <response code="200">Template saved.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("from-event/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveFromEvent(
        Guid eventId,
        [FromBody] SavePlannerTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var plannerId = GetPlannerId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var result = await _mediator.Send(new SavePlannerTaskTemplateFromEventCommand
        {
            PlannerId = plannerId,
            EventId = eventId,
            Name = request.Name,
            Description = request.Description
        }, cancellationToken);

        return Ok(new
        {
            templateId = result.TemplateId,
            taskCount = result.TaskCount,
            message = $"Saved {result.TaskCount} tasks as template \"{request.Name}\"."
        });
    }

    /// <summary>
    /// Applies a task template to an event.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="eventId">The target event identifier.</param>
    /// <param name="request">Whether to replace existing tasks.</param>
    /// <response code="200">Template applied.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("{templateId:guid}/apply/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Apply(
        Guid templateId,
        Guid eventId,
        [FromBody] ApplyPlannerTaskTemplateRequest? request,
        CancellationToken cancellationToken)
    {
        var plannerId = GetPlannerId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        var result = await _mediator.Send(new ApplyPlannerTaskTemplateCommand
        {
            PlannerId = plannerId,
            EventId = eventId,
            TemplateId = templateId,
            ReplaceExisting = request?.ReplaceExisting ?? false
        }, cancellationToken);

        return Ok(new
        {
            tasksCreated = result.TasksCreated,
            taskPlanPhase = result.TaskPlanPhase,
            message = $"Applied {result.TasksCreated} tasks from your template."
        });
    }

    /// <summary>
    /// Deletes a task template.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <response code="204">Template deleted.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpDelete("{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid templateId, CancellationToken cancellationToken)
    {
        var plannerId = GetPlannerId();
        if (string.IsNullOrWhiteSpace(plannerId))
            return Unauthorized();

        await _mediator.Send(new DeletePlannerTaskTemplateCommand
        {
            PlannerId = plannerId,
            TemplateId = templateId
        }, cancellationToken);

        return NoContent();
    }
}

/// <summary>Payload for saving a task template from an event.</summary>
public record SavePlannerTaskTemplateRequest(string Name, string? Description);

/// <summary>Payload for applying a task template.</summary>
public record ApplyPlannerTaskTemplateRequest(bool ReplaceExisting = false);
