using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/planner/ai")]
[Authorize]
public class PlannerAiController : ControllerBase
{
    private readonly IAiCopilotService _aiCopilotService;
    private readonly IMediator _mediator;
    private readonly IPlannerVendorSuggestionService _vendorSuggestionService;
    private readonly IWeddingEventRepository _eventRepository;

    public PlannerAiController(
        IAiCopilotService aiCopilotService,
        IMediator mediator,
        IPlannerVendorSuggestionService vendorSuggestionService,
        IWeddingEventRepository eventRepository)
    {
        _aiCopilotService = aiCopilotService;
        _mediator = mediator;
        _vendorSuggestionService = vendorSuggestionService;
        _eventRepository = eventRepository;
    }

    [HttpPost("suggest-tasks")]
    public async Task<IActionResult> SuggestTasks(
        [FromBody] SuggestTasksRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _aiCopilotService.SummarizeMeetingToTasksAsync(
            new MeetingSummaryRequest(request.EventId, request.EventName, request.Notes),
            cancellationToken);

        return Ok(new
        {
            result.ExecutiveSummary,
            proposedTasks = result.ProposedTasks,
            result.IsSimulated
        });
    }

    [HttpPost("suggest-vendors")]
    public async Task<IActionResult> SuggestVendors(
        [FromBody] SuggestVendorsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
            return NotFound();

        var canManage = weddingEvent.ManagingPlannerId == userId
            || weddingEvent.CreatedById == userId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, userId, cancellationToken);
        if (!canManage)
            return Forbid();

        var recommendations = await _vendorSuggestionService.BuildSuggestionsAsync(
            request.EventId,
            request.Category,
            request.TopN <= 0 ? 5 : request.TopN,
            cancellationToken);

        return Ok(recommendations);
    }

    [HttpPost("personalize-checklist-plan")]
    public async Task<IActionResult> PersonalizeChecklistPlan(
        [FromBody] PersonalizeChecklistPlanRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GeneratePersonalizedChecklistPlanCommand
        {
            EventId = request.EventId,
            UserId = userId,
            MeetingNotesOrTranscript = request.MeetingNotesOrTranscript
        });

        return Ok(result);
    }
}

public record PersonalizeChecklistPlanRequest(Guid EventId, string? MeetingNotesOrTranscript);

public record SuggestTasksRequest(
    Guid EventId,
    string EventName,
    string Notes);

public record SuggestVendorsRequest(
    Guid EventId,
    string Category,
    int TopN = 5);
