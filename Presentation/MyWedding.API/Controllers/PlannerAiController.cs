using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Planner AI copilot: task suggestions, vendor recommendations, and checklist personalization.
/// </summary>
[ApiController]
[Route("api/planner/ai")]
[Authorize]
public class PlannerAiController : ControllerBase
{
    private readonly IAiCopilotService _aiCopilotService;
    private readonly IMediator _mediator;
    private readonly IPlannerVendorSuggestionService _vendorSuggestionService;
    private readonly IWeddingEventRepository _eventRepository;
        /// <summary>
        /// Initializes a new instance of the <see cref="PlannerAiController"/> class.
        /// </summary>
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

    /// <summary>
    /// Summarizes meeting notes and proposes tasks for an event.
    /// </summary>
    /// <param name="request">Event ID, name, and meeting notes.</param>
    /// <response code="200">Proposed tasks and executive summary returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("suggest-tasks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Suggests vendors for an event category based on brief and availability.
    /// </summary>
    /// <param name="request">Event ID, vendor category, and number of suggestions.</param>
    /// <response code="200">Vendor suggestions returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller cannot manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("suggest-vendors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Personalizes a checklist plan using meeting notes or transcripts.
    /// </summary>
    /// <param name="request">Event ID and meeting notes.</param>
    /// <response code="200">Personalized checklist plan returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("personalize-checklist-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

/// <summary>Payload for personalizing a checklist plan.</summary>
public record PersonalizeChecklistPlanRequest(Guid EventId, string? MeetingNotesOrTranscript);

/// <summary>Payload for AI task suggestions from meeting notes.</summary>
public record SuggestTasksRequest(
    Guid EventId,
    string EventName,
    string Notes);

/// <summary>Payload for AI vendor suggestions.</summary>
public record SuggestVendorsRequest(
    Guid EventId,
    string Category,
    int TopN = 5);
