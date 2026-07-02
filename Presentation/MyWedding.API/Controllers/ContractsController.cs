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

/// <summary>
/// Booking contract lifecycle: upload, send, sign, and download PDF contracts.
/// </summary>
[ApiController]
[Route("api/bookings")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractsController"/> class.
        /// </summary>
    public ContractsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves contract metadata for a booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>Contract status, parties, and file reference.</returns>
    /// <response code="200">Contract metadata returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">Booking or contract not found.</response>
    [HttpGet("{bookingId:guid}/contract")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Downloads the contract PDF file for a booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>PDF file stream.</returns>
    /// <response code="200">PDF file returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">Contract not found.</response>
    [HttpGet("{bookingId:guid}/contract/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Uploads a custom PDF contract or generates a standard contract for a booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="file">Optional PDF file (max 10 MB).</param>
    /// <param name="generateStandardContract">When true, generates a platform standard contract.</param>
    /// <returns>Uploaded contract details.</returns>
    /// <response code="200">Contract uploaded or generated.</response>
    /// <response code="400">Invalid file type or missing content.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("{bookingId:guid}/contract/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Sends the contract to the client for review and signature.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>Send confirmation and notification status.</returns>
    /// <response code="200">Contract sent successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("{bookingId:guid}/contract/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Signs the booking contract (client or vendor action).
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="request">Signer name and optional contract file URL.</param>
    /// <returns>Signed contract details.</returns>
    /// <response code="200">Contract signed successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost("{bookingId:guid}/contract/sign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

/// <summary>Payload for signing a booking contract.</summary>
public record SignBookingContractRequest(string SignerName, string? ContractFileUrl);
