// File: src/Presentation/MyWedding.API/Controllers/AuthController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Users.Commands.SyncUser;
using System.Security.Claims;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;

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

        // --- BULLETPROOF FIX ---
        UserRecord? userRecord = null;
        var displayName = "";

        if (FirebaseApp.DefaultInstance != null)
        {
            try
            {
                userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseUid);
                displayName = userRecord?.DisplayName ?? "";
            }
            catch (FirebaseAuthException)
            {
                displayName = User.FindFirstValue("name") ?? "";
            }
        }
        else
        {
            Console.WriteLine("ERROR: FirebaseApp.DefaultInstance is null. SDK may not be initialized.");
            displayName = User.FindFirstValue("name") ?? "";
        }
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
            firstName = nameParts.Length > 0 ? nameParts[0] : "User";
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