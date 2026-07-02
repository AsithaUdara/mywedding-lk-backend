using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.AcceptVendorBooking;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.DeclineVendorBooking;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.ApproveVendorFromShortlist;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.CreateVendorShortlist;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.RequestVendorBooking;
using MyWedding.Vendors.Application.Features.Shortlist.Commands.SendShortlistToClient;
using MyWedding.Vendors.Application.Features.Shortlist.Queries.GetVendorShortlistByEvent;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Vendor shortlist workflow: planner curates vendors, client approves, bookings are requested.
/// </summary>
[ApiController]
[Authorize]
public class VendorShortlistController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorShortlistController"/> class.
        /// </summary>
    public VendorShortlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Returns the vendor shortlist for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <response code="200">Shortlist items returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("api/events/{eventId:guid}/vendor-shortlist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetShortlist(Guid eventId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var items = await _mediator.Send(new GetVendorShortlistByEventQuery
        {
            EventId = eventId,
            UserId = userId
        });
        return Ok(items);
    }

    /// <summary>
    /// Creates a vendor shortlist for an event (planner action).
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">Shortlist items and whether to send to client immediately.</param>
    /// <response code="200">Shortlist created.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/planner/events/{eventId:guid}/vendor-shortlist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateShortlist(
        Guid eventId,
        [FromBody] CreateVendorShortlistRequest request)
    {
        var plannerId = GetUserId();
        if (string.IsNullOrEmpty(plannerId))
            return Unauthorized();

        var ids = await _mediator.Send(new CreateVendorShortlistCommand
        {
            EventId = eventId,
            PlannerId = plannerId,
            SendToClient = request.SendToClient,
            Items = request.Items.Select(i => new CreateVendorShortlistItemRequest(
                i.VendorServiceId,
                i.CategoryLabel,
                i.PlannerNotes,
                i.ProposedAmount,
                i.ServiceDate)).ToList()
        });

        return Ok(new { itemIds = ids });
    }

    /// <summary>
    /// Sends the shortlist (or selected items) to the client for review.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="request">Optional list of item IDs to send; sends all if omitted.</param>
    /// <response code="200">Shortlist sent to client.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/planner/events/{eventId:guid}/vendor-shortlist/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendToClient(Guid eventId, [FromBody] SendShortlistRequest? request)
    {
        var plannerId = GetUserId();
        if (string.IsNullOrEmpty(plannerId))
            return Unauthorized();

        await _mediator.Send(new SendShortlistToClientCommand
        {
            EventId = eventId,
            PlannerId = plannerId,
            ItemIds = request?.ItemIds
        });
        return Ok(new { message = "Shortlist sent to client." });
    }

    /// <summary>
    /// Approves or rejects a shortlist item (client action).
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="itemId">The shortlist item identifier.</param>
    /// <param name="request">Set Reject to true to reject the vendor.</param>
    /// <response code="200">Item approved or rejected.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/events/{eventId:guid}/vendor-shortlist/{itemId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Approve(Guid eventId, Guid itemId, [FromBody] ApproveShortlistRequest? request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var resultId = await _mediator.Send(new ApproveVendorFromShortlistCommand
        {
            EventId = eventId,
            ShortlistItemId = itemId,
            ClientUserId = userId,
            Reject = request?.Reject ?? false
        });

        return Ok(new { shortlistItemId = resultId });
    }

    /// <summary>
    /// Requests a formal booking from an approved shortlist item.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="itemId">The shortlist item identifier.</param>
    /// <response code="200">Booking request created.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/events/{eventId:guid}/vendor-shortlist/{itemId:guid}/request-booking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestBooking(Guid eventId, Guid itemId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var bookingId = await _mediator.Send(new RequestVendorBookingCommand
        {
            EventId = eventId,
            ShortlistItemId = itemId,
            UserId = userId
        });

        return Ok(new { bookingId });
    }

    /// <summary>
    /// Accepts a booking request (vendor action).
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <response code="200">Booking accepted.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/bookings/{bookingId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AcceptBooking(Guid bookingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _mediator.Send(new AcceptVendorBookingCommand
        {
            BookingId = bookingId,
            VendorUserId = userId
        });

        return Ok(new { message = "Booking accepted." });
    }

    /// <summary>
    /// Declines a booking request (vendor action).
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <response code="200">Booking declined.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("api/bookings/{bookingId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeclineBooking(Guid bookingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _mediator.Send(new DeclineVendorBookingCommand
        {
            BookingId = bookingId,
            VendorUserId = userId
        });

        return Ok(new { message = "Booking declined." });
    }
}

/// <summary>Payload for creating a vendor shortlist.</summary>
public record CreateVendorShortlistRequest(
    bool SendToClient,
    IReadOnlyList<CreateVendorShortlistItemBody> Items);

/// <summary>A single vendor service entry in a shortlist.</summary>
public record CreateVendorShortlistItemBody(
    Guid VendorServiceId,
    string? CategoryLabel,
    string? PlannerNotes,
    decimal ProposedAmount,
    DateTime? ServiceDate);

/// <summary>Payload for sending shortlist items to the client.</summary>
public record SendShortlistRequest(IReadOnlyList<Guid>? ItemIds);

/// <summary>Payload for approving or rejecting a shortlist item.</summary>
public record ApproveShortlistRequest(bool Reject = false);
