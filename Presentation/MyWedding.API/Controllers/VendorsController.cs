using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Vendors.Commands.RegisterVendor;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorById;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorCategories;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetVendors([FromQuery] GetVendorsQuery query)
    {
        var vendors = await _mediator.Send(query);
        return Ok(vendors);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _mediator.Send(new GetVendorCategoriesQuery());
        return Ok(categories);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVendorById(string id)
    {
        var query = new GetVendorByIdQuery { VendorId = id };
        var vendor = await _mediator.Send(query);
        return vendor is not null ? Ok(vendor) : NotFound();
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> RegisterVendor([FromBody] RegisterVendorCommand command)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        command.UserId = userId;
        var vendorId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetVendorById), new { id = vendorId }, new { VendorId = vendorId });
    }
}
