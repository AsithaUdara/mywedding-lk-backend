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

    public PlannerAiController(IAiCopilotService aiCopilotService)
    {
        _aiCopilotService = aiCopilotService;
    }

    [HttpPost("draft-inquiry")]
    public async Task<IActionResult> DraftInquiry([FromBody] DraftInquiryEmailRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _aiCopilotService.DraftInquiryEmailAsync(
            new InquiryEmailDraftRequest(
                request.PlannerName,
                request.VendorBusinessName,
                request.VendorCategory,
                request.EventName,
                request.WeddingDate,
                request.Venue,
                request.BudgetLkr,
                request.StyleNotes,
                request.ServiceRequirements),
            cancellationToken);

        return Ok(new
        {
            result.Subject,
            result.Body,
            result.IsSimulated
        });
    }

    [HttpPost("summarize-meeting")]
    public async Task<IActionResult> SummarizeMeeting(
        [FromBody] SummarizeMeetingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _aiCopilotService.SummarizeMeetingToTasksAsync(
            new MeetingSummaryRequest(request.EventId, request.EventName, request.MeetingNotesOrTranscript),
            cancellationToken);

        return Ok(new
        {
            result.ExecutiveSummary,
            proposedTasks = result.ProposedTasks,
            result.IsSimulated
        });
    }
}

public record DraftInquiryEmailRequest(
    string PlannerName,
    string VendorBusinessName,
    string VendorCategory,
    string EventName,
    DateOnly WeddingDate,
    string? Venue,
    decimal? BudgetLkr,
    string? StyleNotes,
    IReadOnlyList<string>? ServiceRequirements);

public record SummarizeMeetingRequest(
    Guid EventId,
    string EventName,
    string MeetingNotesOrTranscript);
