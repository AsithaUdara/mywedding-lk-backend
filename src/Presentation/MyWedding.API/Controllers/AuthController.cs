using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Users.Commands.SyncUser;
using System.Security.Claims;
using System.Threading.Tasks;
using FirebaseAdmin.Auth; // <-- ADD THIS

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

        if (string.IsNullOrEmpty(firebaseUid) || string.IsNullOrEmpty(email))
        {
            return BadRequest("Invalid token claims.");
        }

        // --- THE FIX IS HERE ---
        // Get the full user record directly from Firebase to ensure we have the latest displayName
        UserRecord userRecord;
        try
        {
            userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseUid);
        }
        catch (FirebaseAuthException ex)
        {
            return BadRequest($"Failed to retrieve user from Firebase: {ex.Message}");
        }
        
        var displayName = userRecord.DisplayName ?? "";
        // --- END OF FIX ---

        string firstName;
        string lastName;
        
        var nameParts = displayName.Trim().Split(' ', 2);
        
        if (nameParts.Length > 1)
        {
            firstName = nameParts[0];
            lastName = nameParts[1];
        }
        else
        {
            firstName = nameParts.Length > 0 && !string.IsNullOrWhiteSpace(nameParts[0]) ? nameParts[0] : "User";
            lastName = "";
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