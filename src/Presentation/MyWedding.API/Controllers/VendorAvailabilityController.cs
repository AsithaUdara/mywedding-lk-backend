using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/vendor/availability")]
[Authorize]
public class VendorAvailabilityController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IVendorBlockedDateRepository _blockedDateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorAvailabilityController(
        ApplicationDbContext db,
        IVendorBlockedDateRepository blockedDateRepository,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _blockedDateRepository = blockedDateRepository;
        _unitOfWork = unitOfWork;
    }

    private string? GetVendorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> GetAvailability([FromQuery] string month, CancellationToken cancellationToken)
    {
        var vendorId = GetVendorId();
        if (string.IsNullOrEmpty(vendorId))
            return Unauthorized();

        if (!TryParseMonth(month, out var year, out var monthIndex))
            return BadRequest(new { message = "Invalid month. Use yyyy-MM format." });

        var monthStart = new DateTime(year, monthIndex, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var bookedDates = await _db.VendorBookings
            .AsNoTracking()
            .Join(
                _db.VendorServices.Where(s => s.VendorId == vendorId),
                b => b.ServiceId,
                s => s.Id,
                (b, _) => b)
            .Where(b =>
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                && b.ServiceDate >= monthStart
                && b.ServiceDate < monthEnd)
            .Select(b => b.ServiceDate.Day)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        var blocked = await _blockedDateRepository.GetByVendorAndMonthAsync(
            vendorId,
            year,
            monthIndex,
            cancellationToken);

        return Ok(new VendorAvailabilityResponse(
            bookedDates,
            blocked.Select(b => b.Date.Day).Distinct().OrderBy(d => d).ToList(),
            blocked.Select(b => new BlockedDateDetail(b.Date.ToString("yyyy-MM-dd"), b.Reason)).ToList()
        ));
    }

    [HttpPost("block")]
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

    [HttpDelete("block/{date}")]
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

public record BlockVendorDateRequest(string Date, string? Reason);
public record BlockedDateDetail(string Date, string? Reason);
public record VendorAvailabilityResponse(
    IReadOnlyList<int> BookedDates,
    IReadOnlyList<int> BlockedDates,
    IReadOnlyList<BlockedDateDetail> BlockedDateDetails);
