using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Interfaces;

namespace MyWedding.API.Controllers;

/// <summary>
/// One-time bootstrap for assigning Firebase custom roles (e.g. first admin).
/// Protected by AdminBootstrap:Secret — not for production use without rotating the secret.
/// </summary>
[ApiController]
[Route("api/admin/bootstrap")]
[AllowAnonymous]
public class AdminBootstrapController : ControllerBase
{
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly IConfiguration _configuration;

    public AdminBootstrapController(IFirebaseAuthService firebaseAuthService, IConfiguration configuration)
    {
        _firebaseAuthService = firebaseAuthService;
        _configuration = configuration;
    }

    /// <summary>
    /// Sets Firebase custom claim role for a user (admin, vendor, planner, user).
    /// Requires header X-Bootstrap-Secret matching AdminBootstrap:Secret in configuration.
    /// </summary>
    [HttpPost("set-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetRole(
        [FromBody] SetRoleRequest request,
        [FromHeader(Name = "X-Bootstrap-Secret")] string? bootstrapSecret,
        CancellationToken cancellationToken)
    {
        var expectedSecret = _configuration["AdminBootstrap:Secret"];
        if (string.IsNullOrWhiteSpace(expectedSecret) ||
            !string.Equals(bootstrapSecret, expectedSecret, StringComparison.Ordinal))
        {
            return Unauthorized(new { message = "Invalid or missing X-Bootstrap-Secret." });
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { message = "userId is required." });

        var allowedRoles = new[] { "admin", "vendor", "planner", "user" };
        if (!allowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "role must be admin, vendor, planner, or user." });

        await _firebaseAuthService.SetUserRoleAsync(request.UserId, request.Role.ToLowerInvariant());

        return Ok(new
        {
            message = $"Firebase custom claim 'role' set to '{request.Role.ToLowerInvariant()}' for user {request.UserId}.",
            hint = "User must sign out and sign in again (or refresh token) before the new role appears in the app."
        });
    }
}

public class SetRoleRequest
{
    public required string UserId { get; set; }
    public required string Role { get; set; }
}
