using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;



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
    private readonly ApplicationDbContext _db;

    public VendorsController(IMediator mediator, ApplicationDbContext db)
    {
        _mediator = mediator;
        _db = db;
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
        var vendorIds = vendors.Select(v => v.UserId).ToList();
        var activeSubscriptions = await _db.VendorSubscriptions
            .Where(s => vendorIds.Contains(s.VendorId) && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var subscriptionByVendor = activeSubscriptions
            .GroupBy(s => s.VendorId)
            .ToDictionary(g => g.Key, g => g.First());

        var tierWeight = new Dictionary<SubscriptionPlanTier, int>
        {
            [SubscriptionPlanTier.Sponsored] = 3,
            [SubscriptionPlanTier.Featured] = 2,
            [SubscriptionPlanTier.Free] = 1,
            [SubscriptionPlanTier.PlannerPro] = 1
        };

        var ranked = vendors
            .Select(v =>
            {
                var tier = subscriptionByVendor.TryGetValue(v.UserId, out var sub)
                    ? sub.Tier
                    : SubscriptionPlanTier.Free;
                return new
                {
                    v.UserId,
                    v.BusinessName,
                    v.BusinessDescription,
                    v.WebsiteUrl,
                    v.City,
                    v.VerificationStatus,
                    v.AverageRating,
                    v.CategoryName,
                    premiumTier = tier.ToString(),
                    isSponsored = tier == SubscriptionPlanTier.Sponsored,
                    isFeatured = tier == SubscriptionPlanTier.Featured
                };
            })
            .OrderByDescending(v => tierWeight.TryGetValue(Enum.Parse<SubscriptionPlanTier>(v.premiumTier), out var weight) ? weight : 1)
            .ThenByDescending(v => v.AverageRating)
            .ToList();

        return Ok(ranked);
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
