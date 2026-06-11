using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.SharedKernel;


using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers;

/// <summary>
/// Handles authentication-related operations such as syncing a Firebase user to the local database.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AuthController(IMediator mediator, IFirebaseAuthService firebaseAuthService)
    {
        _mediator = mediator;
        _firebaseAuthService = firebaseAuthService;
    }

    /// <summary>
    /// Syncs the Firebase-authenticated user into the local database.
    /// Creates the user record on first login; subsequent calls are idempotent.
    /// The user's display name is sourced from their JWT claims.
    /// </summary>
    /// <returns>The internal user ID.</returns>
    [HttpPost("sync-user")]
    public async Task<IActionResult> SyncUser()
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(firebaseUid) || string.IsNullOrEmpty(email))
        {
            return BadRequest(new { message = "Invalid token claims. Ensure the Firebase ID token is valid." });
        }

        // Resolve display name from JWT claims — avoids a redundant Admin SDK round-trip per login
        var displayName = User.FindFirstValue("name") ?? string.Empty;

        var nameParts = displayName.Trim().Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0
            ? nameParts[0]
            : UserDisplayNameHelper.NameFromEmail(email);
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

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
