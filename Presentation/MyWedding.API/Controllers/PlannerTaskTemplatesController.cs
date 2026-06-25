using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Planner.Application.Features.Planner.TaskTemplates;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/planner/task-templates")]
[Authorize]
public class PlannerTaskTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlannerTaskTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetPlannerId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
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

    [HttpPost("from-event/{eventId:guid}")]
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

    [HttpPost("{templateId:guid}/apply/{eventId:guid}")]
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

    [HttpDelete("{templateId:guid}")]
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

public record SavePlannerTaskTemplateRequest(string Name, string? Description);

public record ApplyPlannerTaskTemplateRequest(bool ReplaceExisting = false);
