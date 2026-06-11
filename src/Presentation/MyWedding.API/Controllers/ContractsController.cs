using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Contracts.Commands.SendBookingContract;
using MyWedding.Vendors.Application.Features.Contracts.Commands.SignBookingContract;
using MyWedding.Vendors.Application.Features.Contracts.Commands.UploadBookingContract;
using MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContract;
using MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContractFile;
using System.Security.Claims;

namespace MyWedding.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContractsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{bookingId:guid}/contract")]
    public async Task<IActionResult> GetContract(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetBookingContractQuery
        {
            BookingId = bookingId,
            UserId = userId
        }, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Booking not found." });

        return Ok(result);
    }

    [HttpGet("{bookingId:guid}/contract/file")]
    public async Task<IActionResult> GetContractFile(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetBookingContractFileQuery
        {
            BookingId = bookingId,
            UserId = userId
        }, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Contract not found." });

        return File(result.PdfBytes, result.ContentType, result.FileName);
    }

    [HttpPost("{bookingId:guid}/contract/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadContract(
        Guid bookingId,
        IFormFile? file,
        [FromForm] bool generateStandardContract = false,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        byte[]? pdfBytes = null;
        if (file is not null)
        {
            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only PDF files are supported." });
            }

            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            pdfBytes = memory.ToArray();
        }

        var result = await _mediator.Send(new UploadBookingContractCommand
        {
            BookingId = bookingId,
            VendorUserId = userId,
            PdfBytes = pdfBytes,
            GenerateStandardContract = generateStandardContract
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{bookingId:guid}/contract/send")]
    public async Task<IActionResult> SendContract(Guid bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new SendBookingContractCommand
        {
            BookingId = bookingId,
            VendorUserId = userId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{bookingId:guid}/contract/sign")]
    public async Task<IActionResult> SignContract(
        Guid bookingId,
        [FromBody] SignBookingContractRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _mediator.Send(new SignBookingContractCommand
        {
            BookingId = bookingId,
            UserId = userId,
            SignerName = request.SignerName,
            ClientIpAddress = clientIp,
            ContractFileUrl = request.ContractFileUrl
        }, cancellationToken);

        return Ok(result);
    }
}

public record SignBookingContractRequest(string SignerName, string? ContractFileUrl);
