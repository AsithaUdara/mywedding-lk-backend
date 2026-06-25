using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
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
        [HttpGet("vendor")]
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

        [HttpGet("event/{eventId}")]
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

        [HttpPatch("{id}/status")]
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

        [HttpPost("{id}/approve")]
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

    public record UpdateBookingStatusRequest(MyWedding.Domain.Enums.BookingStatus Status);

    public record CreateBookingRequest(
        Guid EventId,
        Guid ServiceId,
        decimal FinalAmount,
        DateTime ServiceDate
    );
}
