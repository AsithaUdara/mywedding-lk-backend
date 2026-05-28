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

[ApiController]
[Route("api/vendor/inquiries")]
[Authorize]
public class VendorInquiriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorInquiriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetInquiries()
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var result = await _mediator.Send(new GetVendorInquiriesQuery { VendorId = vendorId });
        return Ok(result);
    }

    [HttpPost("{id:guid}/quote")]
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

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        await _mediator.Send(new MarkInquiryAsReadCommand { InquiryId = id, VendorId = vendorId });
        return NoContent();
    }
}

public record GenerateInquiryQuoteRequest(decimal? ProposedAmount);
