using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers;

/// <summary>
/// Vendor performance analytics: profile views, inquiry trends, and booking win rate.
/// </summary>
[ApiController]
[Route("api/vendor/analytics")]
[Authorize]
public class VendorAnalyticsController : ControllerBase
{
    private readonly IVendorAnalyticsRepository _analyticsRepository;
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorAnalyticsController"/> class.
        /// </summary>
    public VendorAnalyticsController(IVendorAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Returns weekly profile view counts for the authenticated vendor.
    /// </summary>
    /// <param name="weeks">Number of weeks to include (default 6).</param>
    /// <response code="200">Profile view data returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("profile-views")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileViews([FromQuery] int weeks = 6, CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetProfileViewsByWeekAsync(vendorId, weeks, cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Returns monthly inquiry counts for the authenticated vendor.
    /// </summary>
    /// <param name="months">Number of months to include (default 6).</param>
    /// <response code="200">Inquiry trend data returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("inquiries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInquiryTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetInquiriesByMonthAsync(vendorId, months, cancellationToken);
        return Ok(data);
    }

    /// <summary>
    /// Returns the vendor's booking win rate (inquiries converted to confirmed bookings).
    /// </summary>
    /// <response code="200">Win rate data returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet("win-rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWinRate(CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetWinRateAsync(vendorId, cancellationToken);
        return Ok(data);
    }
}
