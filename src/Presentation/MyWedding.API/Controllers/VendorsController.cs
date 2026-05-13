using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Provides public and authenticated endpoints for browsing and registering vendors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a paginated, filterable list of vendors.
    /// This is a public endpoint — no authentication required.
    /// </summary>
    /// <param name="query">Optional filter parameters (category, location).</param>
    /// <returns>A list of vendor summaries.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetVendors([FromQuery] GetVendorsQuery query)
    {
        var vendors = await _mediator.Send(query);
        return Ok(vendors);
    }

    /// <summary>
    /// Retrieves the full profile of a specific vendor by their user ID.
    /// This is a public endpoint — no authentication required.
    /// </summary>
    /// <param name="id">The vendor's user ID.</param>
    /// <returns>The vendor profile, or 404 if not found.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVendorById(string id)
    {
        var query = new GetVendorByIdQuery { VendorId = id };
        var vendor = await _mediator.Send(query);

        return vendor is not null ? Ok(vendor) : NotFound();
    }

    /// <summary>
    /// Registers a new vendor profile for the currently authenticated user.
    /// The user must be authenticated via Firebase before calling this endpoint.
    /// </summary>
    /// <param name="command">The vendor registration details.</param>
    /// <returns>200 OK with the new vendor ID on success.</returns>
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> RegisterVendor([FromBody] RegisterVendorCommand command)
    {
        // Ensure the command always uses the authenticated user's ID, not a client-supplied value.
        // This prevents a user from registering a vendor profile on behalf of another user.
        command.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Unable to resolve user identity from token.");

        var vendorId = await _mediator.Send(command);

        // Domain exceptions (e.g., duplicate registration) are handled by ExceptionMiddleware
        return Ok(new { vendorId });
    }
}
