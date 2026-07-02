using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Inquiries.Commands.MarkInquiryAsRead;
using MyWedding.Vendors.Application.Features.Inquiries.Commands.SendInquiry;
using MyWedding.Vendors.Application.Features.Inquiries.Queries.GetVendorInquiries;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Vendor inquiries: couples send messages; vendors view and mark as read.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class InquiriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="InquiriesController"/> class.
        /// </summary>
        public InquiriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Sends an inquiry message to a vendor from an authenticated couple or organizer.
        /// </summary>
        /// <param name="vendorId">The vendor to contact.</param>
        /// <param name="request">The inquiry message body.</param>
        /// <returns>The ID of the created inquiry.</returns>
        /// <response code="200">Inquiry sent.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPost("vendors/{vendorId}/inquiries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendInquiry(string vendorId, [FromBody] SendInquiryRequest request)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(senderEmail))
            {
                return Unauthorized();
            }

            var command = new SendInquiryCommand
            {
                VendorId = vendorId,
                Message = request.Message,
                SenderId = senderId,
                SenderEmail = senderEmail
            };

            var inquiryId = await _mediator.Send(command);
            return Ok(new { InquiryId = inquiryId });
        }

        /// <summary>
        /// Lists all inquiries received by the authenticated vendor.
        /// </summary>
        /// <returns>Inquiry inbox for the vendor.</returns>
        /// <response code="200">Inquiries returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpGet("vendor/dashboard/inquiries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetVendorInquiries()
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId)) return Unauthorized();

            var query = new GetVendorInquiriesQuery { VendorId = vendorId };
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Marks a vendor inquiry as read.
        /// </summary>
        /// <param name="id">The inquiry identifier.</param>
        /// <response code="204">Inquiry marked as read.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [Authorize]
        [HttpPatch("vendor/dashboard/inquiries/{id}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkInquiryAsRead(Guid id)
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId)) return Unauthorized();

            var command = new MarkInquiryAsReadCommand
            {
                InquiryId = id,
                VendorId = vendorId
            };

            await _mediator.Send(command);
            return NoContent();
        }
    }

    /// <summary>Payload for sending a vendor inquiry.</summary>
    public class SendInquiryRequest
    {
        /// <summary>The inquiry message text.</summary>
        public required string Message { get; set; }
    }
}
