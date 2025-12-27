using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Bookings.Commands.CreateBooking;
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

            try
            {
                var bookingId = await _mediator.Send(command);
                return Ok(new { BookingId = bookingId });
            }
            catch (MyWedding.Application.Common.Exceptions.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (MyWedding.Application.Common.Exceptions.ForbiddenAccessException)
            {
                return Forbid();
            }
        }
    }

    public record CreateBookingRequest(
        Guid EventId,
        Guid ServiceId,
        decimal FinalAmount,
        DateTime ServiceDate
    );
}
