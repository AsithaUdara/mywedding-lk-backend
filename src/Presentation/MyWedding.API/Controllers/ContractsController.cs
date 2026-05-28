using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Vendors.Application.Features.Contracts.Commands.SignBookingContract;
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
