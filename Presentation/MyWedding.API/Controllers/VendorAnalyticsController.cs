using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/vendor/analytics")]
[Authorize]
public class VendorAnalyticsController : ControllerBase
{
    private readonly IVendorAnalyticsRepository _analyticsRepository;

    public VendorAnalyticsController(IVendorAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("profile-views")]
    public async Task<IActionResult> GetProfileViews([FromQuery] int weeks = 6, CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetProfileViewsByWeekAsync(vendorId, weeks, cancellationToken);
        return Ok(data);
    }

    [HttpGet("inquiries")]
    public async Task<IActionResult> GetInquiryTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetInquiriesByMonthAsync(vendorId, months, cancellationToken);
        return Ok(data);
    }

    [HttpGet("win-rate")]
    public async Task<IActionResult> GetWinRate(CancellationToken cancellationToken = default)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        var data = await _analyticsRepository.GetWinRateAsync(vendorId, cancellationToken);
        return Ok(data);
    }
}
