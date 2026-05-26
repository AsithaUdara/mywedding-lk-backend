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
    [ApiController]
    [Route("api")]
    public class InquiriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InquiriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Endpoint for couples to send an inquiry to a vendor
        [Authorize]
        [HttpPost("vendors/{vendorId}/inquiries")]
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

        // Endpoint for vendors to get their inquiries
        [Authorize]
        [HttpGet("vendor/dashboard/inquiries")]
        public async Task<IActionResult> GetVendorInquiries()
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId)) return Unauthorized();

            var query = new GetVendorInquiriesQuery { VendorId = vendorId };
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        // Endpoint for vendors to mark an inquiry as read
        [Authorize]
        [HttpPatch("vendor/dashboard/inquiries/{id}/read")]
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

    public class SendInquiryRequest
    {
        public required string Message { get; set; }
    }
}
