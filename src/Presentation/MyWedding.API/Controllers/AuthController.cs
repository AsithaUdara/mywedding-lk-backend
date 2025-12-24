using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Users.Commands.SyncUser;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("sync-user")]
    public async Task<IActionResult> SyncUser()
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue("name") ?? ""; // Firebase often sends name in this claim

        // Basic name splitting, can be improved on the frontend
        var nameParts = name.Split(' ', 2);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";


        if (string.IsNullOrEmpty(firebaseUid) || string.IsNullOrEmpty(email))
        {
            return BadRequest("Invalid token claims.");
        }

        var command = new SyncUserCommand
        {
            FirebaseUid = firebaseUid,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        var userId = await _mediator.Send(command);

        return Ok(new { UserId = userId });
    }
}