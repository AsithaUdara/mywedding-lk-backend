using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Vendors.Application.Features.Availability.Queries.GetVendorAvailability;
using System.Globalization;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

/// <summary>
/// Vendor calendar availability: view booked dates and block/unblock dates.
/// </summary>
[ApiController]
[Route("api/vendor/availability")]
[Authorize]
public class VendorAvailabilityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IVendorBlockedDateRepository _blockedDateRepository;
    private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="VendorAvailabilityController"/> class.
        /// </summary>
    public VendorAvailabilityController(
        IMediator mediator,
        IVendorBlockedDateRepository blockedDateRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _blockedDateRepository = blockedDateRepository;
        _unitOfWork = unitOfWork;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Returns availability for a calendar month (booked and blocked dates).
    /// </summary>
    /// <param name="month">Month in yyyy-MM format.</param>
    /// <response code="200">Availability data returned.</response>
    /// <response code="400">Invalid month format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvailability([FromQuery] string month, CancellationToken cancellationToken)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        if (!TryParseMonth(month, out var year, out var monthIndex))
            return BadRequest(new { message = "Invalid month. Use yyyy-MM format." });

        var availability = await _mediator.Send(new GetVendorAvailabilityQuery
        {
            VendorId = vendorId,
            Year = year,
            Month = monthIndex
        }, cancellationToken);

        return availability is null
            ? BadRequest(new { message = "Invalid month." })
            : Ok(availability);
    }

    /// <summary>
    /// Blocks a date on the vendor's calendar.
    /// </summary>
    /// <param name="request">Date (ISO format) and optional reason.</param>
    /// <response code="200">Date blocked.</response>
    /// <response code="400">Invalid date.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BlockDate([FromBody] BlockVendorDateRequest request, CancellationToken cancellationToken)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        if (!DateTime.TryParse(request.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return BadRequest(new { message = "Invalid date." });

        var date = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);

        var existing = await _blockedDateRepository.GetByVendorAndDateAsync(vendorId, date, cancellationToken);
        if (existing is not null)
            return Ok(new { message = "Date already blocked.", date = date.ToString("yyyy-MM-dd") });

        await _blockedDateRepository.AddAsync(new VendorBlockedDate
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            Date = date,
            Reason = request.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Date blocked.", date = date.ToString("yyyy-MM-dd") });
    }

    /// <summary>
    /// Removes a blocked date from the vendor's calendar.
    /// </summary>
    /// <param name="date">Date to unblock (yyyy-MM-dd).</param>
    /// <response code="204">Date unblocked.</response>
    /// <response code="400">Invalid date format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">Blocked date not found.</response>
    [HttpDelete("block/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockDate(string date, CancellationToken cancellationToken)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return BadRequest(new { message = "Invalid date." });

        var day = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        var blocked = await _blockedDateRepository.GetByVendorAndDateAsync(vendorId, day, cancellationToken);
        if (blocked is null)
            return NotFound(new { message = "Blocked date not found." });

        await _blockedDateRepository.DeleteAsync(blocked, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool TryParseMonth(string? month, out int year, out int monthIndex)
    {
        year = 0;
        monthIndex = 0;
        if (string.IsNullOrWhiteSpace(month))
            return false;

        if (DateTime.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            year = parsed.Year;
            monthIndex = parsed.Month;
            return true;
        }

        return false;
    }
}

/// <summary>Payload for blocking a vendor calendar date.</summary>
public record BlockVendorDateRequest(string Date, string? Reason);
