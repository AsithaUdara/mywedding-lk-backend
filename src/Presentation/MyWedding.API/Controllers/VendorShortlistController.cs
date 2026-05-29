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

[ApiController]
[Authorize]
public class VendorShortlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorShortlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("api/events/{eventId:guid}/vendor-shortlist")]
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

    [HttpPost("api/planner/events/{eventId:guid}/vendor-shortlist")]
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

    [HttpPost("api/planner/events/{eventId:guid}/vendor-shortlist/send")]
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

    [HttpPost("api/events/{eventId:guid}/vendor-shortlist/{itemId:guid}/approve")]
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

    [HttpPost("api/events/{eventId:guid}/vendor-shortlist/{itemId:guid}/request-booking")]
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

    [HttpPost("api/bookings/{bookingId:guid}/accept")]
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

    [HttpPost("api/bookings/{bookingId:guid}/decline")]
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

public record CreateVendorShortlistRequest(
    bool SendToClient,
    IReadOnlyList<CreateVendorShortlistItemBody> Items);

public record CreateVendorShortlistItemBody(
    Guid VendorServiceId,
    string? CategoryLabel,
    string? PlannerNotes,
    decimal ProposedAmount,
    DateTime? ServiceDate);

public record SendShortlistRequest(IReadOnlyList<Guid>? ItemIds);

public record ApproveShortlistRequest(bool Reject = false);
