using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Vendors.Commands.RegisterVendor;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorById;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorCategories;
using MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Public vendor directory: search, categories, profiles, and vendor registration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorsController"/> class.
        /// </summary>
    public VendorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Searches and lists verified vendors with optional filters.
    /// </summary>
    /// <param name="query">Search, category, location, and pagination filters.</param>
    /// <returns>A paginated list of vendor listings.</returns>
    /// <response code="200">Vendors returned successfully.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVendors([FromQuery] GetVendorsQuery query)
    {
        var vendors = await _mediator.Send(query);
        return Ok(vendors);
    }

    /// <summary>
    /// Lists all vendor service categories (e.g. Photography, Catering).
    /// </summary>
    /// <returns>Available vendor categories.</returns>
    /// <response code="200">Categories returned successfully.</response>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _mediator.Send(new GetVendorCategoriesQuery());
        return Ok(categories);
    }

    /// <summary>
    /// Retrieves a single vendor profile by ID.
    /// </summary>
    /// <param name="id">The vendor's unique identifier.</param>
    /// <returns>Vendor profile details.</returns>
    /// <response code="200">Vendor found.</response>
    /// <response code="404">Vendor not found.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVendorById(string id)
    {
        var query = new GetVendorByIdQuery { VendorId = id };
        var vendor = await _mediator.Send(query);
        return vendor is not null ? Ok(vendor) : NotFound();
    }

    /// <summary>
    /// Registers the authenticated user as a vendor on the platform.
    /// </summary>
    /// <param name="command">Business name, category, and profile details.</param>
    /// <returns>The ID of the newly created vendor profile.</returns>
    /// <response code="201">Vendor registered successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
