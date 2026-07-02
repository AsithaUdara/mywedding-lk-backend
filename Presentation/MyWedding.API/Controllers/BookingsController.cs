using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Manages vendor service bookings: creation, listing, status updates, and approval.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="BookingsController"/> class.
        /// </summary>
        public BookingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new vendor service booking for an event.
        /// </summary>
        /// <param name="request">Event, service, amount, and service date.</param>
        /// <returns>The ID of the created booking.</returns>
        /// <response code="200">Booking created successfully.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var command = new CreateBookingCommand
            {
                EventId = request.EventId,
                ServiceId = request.ServiceId,
                FinalAmount = request.FinalAmount,
                ServiceDate = request.ServiceDate,
                UserId = userId
            };

            var bookingId = await _mediator.Send(command);
            return Ok(new { BookingId = bookingId });
        }

        /// <summary>
        /// Lists all bookings for the authenticated vendor.
        /// </summary>
        /// <returns>Bookings assigned to the vendor's services.</returns>
        /// <response code="200">Bookings returned successfully.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("vendor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetVendorBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var query = new GetVendorBookingsQuery { VendorUserId = userId };
            var bookings = await _mediator.Send(query);
            return Ok(bookings);
        }

        /// <summary>
        /// Lists all bookings for a specific wedding event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the wedding event.</param>
        /// <returns>Bookings linked to the event.</returns>
        /// <response code="200">Bookings returned successfully.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("event/{eventId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetEventBookings(Guid eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var query = new GetEventBookingsQuery { EventId = eventId, UserId = userId };
            var bookings = await _mediator.Send(query);
            return Ok(bookings);
        }

        /// <summary>
        /// Updates the status of a booking (vendor action).
        /// </summary>
        /// <param name="id">The unique identifier of the booking.</param>
        /// <param name="request">The new booking status.</param>
        /// <response code="200">Status updated successfully.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var command = new UpdateBookingStatusCommand
            {
                BookingId = id,
                NewStatus = request.Status,
                VendorUserId = userId
            };

            await _mediator.Send(command);
            return Ok();
        }

        /// <summary>
        /// Approves a pending booking (organizer or planner action).
        /// </summary>
        /// <param name="id">The unique identifier of the booking.</param>
        /// <response code="200">Booking approved.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ApproveBooking(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _mediator.Send(new ApproveBookingCommand { BookingId = id, UserId = userId });
            return Ok(new { message = "Booking approved." });
        }
    }

    /// <summary>Payload for updating a booking status.</summary>
    public record UpdateBookingStatusRequest(MyWedding.Domain.Enums.BookingStatus Status);

    /// <summary>Payload for creating a new vendor service booking.</summary>
    public record CreateBookingRequest(
        Guid EventId,
        Guid ServiceId,
        decimal FinalAmount,
        DateTime ServiceDate
    );
}
