using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Inquiries.Commands.GenerateInquiryQuote;
using MyWedding.Vendors.Application.Features.Inquiries.Commands.MarkInquiryAsRead;
using MyWedding.Vendors.Application.Features.Inquiries.Queries.GetVendorInquiries;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers;

/// <summary>
/// Vendor inquiry inbox: list inquiries, generate quotes, and mark as read.
/// </summary>
[ApiController]
[Route("api/vendor/inquiries")]
[Authorize]
public class VendorInquiriesController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorInquiriesController"/> class.
        /// </summary>
    public VendorInquiriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Lists all inquiries for the authenticated vendor.
    /// </summary>
    /// <response code="200">Inquiries returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInquiries()
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var result = await _mediator.Send(new GetVendorInquiriesQuery { VendorId = vendorId });
        return Ok(result);
    }

    /// <summary>
    /// Generates a quote response for an inquiry (optionally with a proposed amount).
    /// </summary>
    /// <param name="id">The inquiry identifier.</param>
    /// <param name="request">Optional proposed quote amount.</param>
    /// <response code="200">Quote generated.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("{id:guid}/quote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateQuote(Guid id, [FromBody] GenerateInquiryQuoteRequest? request)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var result = await _mediator.Send(new GenerateInquiryQuoteCommand
        {
            InquiryId = id,
            VendorId = vendorId,
            ProposedAmount = request?.ProposedAmount
        });

        return Ok(result);
    }

    /// <summary>
    /// Marks an inquiry as read.
    /// </summary>
    /// <param name="id">The inquiry identifier.</param>
    /// <response code="204">Inquiry marked as read.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        await _mediator.Send(new MarkInquiryAsReadCommand { InquiryId = id, VendorId = vendorId });
        return NoContent();
    }
}

/// <summary>Payload for generating an inquiry quote.</summary>
public record GenerateInquiryQuoteRequest(decimal? ProposedAmount);
