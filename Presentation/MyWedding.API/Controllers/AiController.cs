using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// AI-powered event assistance: chat, vendor recommendations, and itinerary generation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IEventAiService _eventAiService;
        /// <summary>
        /// Initializes a new instance of the <see cref="AiController"/> class.
        /// </summary>
    public AiController(IEventAiService eventAiService)
    {
        _eventAiService = eventAiService;
    }

    /// <summary>
    /// Sends a chat message to the event AI assistant.
    /// </summary>
    /// <param name="request">Event ID and user message.</param>
    /// <response code="200">AI response returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller cannot access this event.</response>
    [HttpPost("chat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.ChatAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.Message,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Returns AI-ranked vendor recommendations for an event.
    /// </summary>
    /// <param name="request">Event ID and number of recommendations.</param>
    /// <response code="200">Vendor recommendations returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("vendor-recommendations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetVendorRecommendations(
        [FromBody] VendorRecommendationRequest request,
        CancellationToken cancellationToken)
        => RecommendVendorsCoreAsync(request, cancellationToken);

    /// <summary>
    /// Legacy alias for <see cref="GetVendorRecommendations"/>.
    /// </summary>
    [HttpPost("recommend-vendors")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> RecommendVendors(
        [FromBody] VendorRecommendationRequest request,
        CancellationToken cancellationToken)
        => RecommendVendorsCoreAsync(request, cancellationToken);

    private async Task<IActionResult> RecommendVendorsCoreAsync(
        VendorRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _eventAiService.RecommendVendorsAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.TopN,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Generates a day-of wedding itinerary from event tasks and bookings.
    /// </summary>
    /// <param name="request">Event ID and default service duration in hours.</param>
    /// <response code="200">Generated itinerary returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("itinerary/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateItinerary([FromBody] GenerateItineraryRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.GenerateItineraryAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            request.EventId,
            request.DefaultServiceDurationHours,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Retrieves the saved itinerary for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <response code="200">Itinerary returned.</response>
    /// <response code="404">Itinerary not found.</response>
    [HttpGet("itinerary/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItinerary(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _eventAiService.GetItineraryAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            eventId,
            cancellationToken);

        return MapResult(result);
    }

    /// <summary>
    /// Saves or updates itinerary items for an event.
    /// </summary>
    /// <param name="itineraryId">The itinerary identifier.</param>
    /// <param name="request">Ordered list of itinerary time blocks.</param>
    /// <response code="200">Itinerary saved.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPut("itinerary/{itineraryId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

/// <summary>Payload for an AI chat message.</summary>
public record AiChatRequest(Guid EventId, string Message);

/// <summary>Payload for vendor recommendation request.</summary>
public record VendorRecommendationRequest(Guid EventId, int TopN = 5);

/// <summary>A single vendor recommendation result.</summary>
public record VendorRecommendationDto(string VendorId, string BusinessName, decimal Score, string Reason);

/// <summary>Payload for itinerary generation.</summary>
public record GenerateItineraryRequest(Guid EventId, int DefaultServiceDurationHours = 2);

/// <summary>Payload for saving an itinerary.</summary>
public record SaveItineraryRequest(IReadOnlyCollection<SaveItineraryItem> Items);

/// <summary>A single itinerary time block.</summary>
public record SaveItineraryItem(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);
