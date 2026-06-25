using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IEventAiService _eventAiService;

    public AiController(IEventAiService eventAiService)
    {
        _eventAiService = eventAiService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.ChatAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.Message,
            cancellationToken);

        return MapResult(result);
    }

    [HttpPost("recommend-vendors")]
    public async Task<IActionResult> RecommendVendors([FromBody] VendorRecommendationRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.RecommendVendorsAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.TopN,
            cancellationToken);

        return MapResult(result);
    }

    [HttpPost("itinerary/generate")]
    public async Task<IActionResult> GenerateItinerary([FromBody] GenerateItineraryRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.GenerateItineraryAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.DefaultServiceDurationHours,
            cancellationToken);

        return MapResult(result);
    }

    [HttpGet("itinerary/{eventId:guid}")]
    public async Task<IActionResult> GetItinerary(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.GetItineraryAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            eventId,
            cancellationToken);

        return MapResult(result);
    }

    [HttpPut("itinerary/{itineraryId:guid}")]
    public async Task<IActionResult> SaveItinerary(Guid itineraryId, [FromBody] SaveItineraryRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new EventAiSaveItineraryItem(i.Title, i.Description, i.StartsAt, i.EndsAt))
            .ToList();

        var result = await _eventAiService.SaveItineraryAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            itineraryId,
            items,
            cancellationToken);

        return MapResult(result);
    }

    private IActionResult MapResult(EventAiWorkflowResult result) => result.StatusCode switch
    {
        200 => result.Body is null ? Ok() : Ok(result.Body),
        400 => BadRequest(result.Body),
        401 => result.Body is null ? Unauthorized() : Unauthorized(result.Body),
        403 => Forbid(),
        404 => result.Body is null ? NotFound() : NotFound(result.Body),
        _ => StatusCode(result.StatusCode, result.Body)
    };
}

public record AiChatRequest(Guid EventId, string Message);
public record VendorRecommendationRequest(Guid EventId, int TopN = 5);
public record VendorRecommendationDto(string VendorId, string BusinessName, decimal Score, string Reason);
public record GenerateItineraryRequest(Guid EventId, int DefaultServiceDurationHours = 2);
public record SaveItineraryRequest(IReadOnlyCollection<SaveItineraryItem> Items);
public record SaveItineraryItem(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);
