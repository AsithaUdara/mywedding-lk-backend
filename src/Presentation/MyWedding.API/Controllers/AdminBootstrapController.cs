using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Domain.Interfaces;

namespace MyWedding.API.Controllers;

/// <summary>
/// One-time bootstrap for assigning Firebase custom roles (e.g. first admin).
/// Available only in Development, or in any environment when X-Bootstrap-Secret matches AdminBootstrap:Secret.
/// </summary>
[ApiController]
[Route("api/admin/bootstrap")]
[AllowAnonymous]
public class AdminBootstrapController : ControllerBase
{
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AdminBootstrapController(
        IFirebaseAuthService firebaseAuthService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _firebaseAuthService = firebaseAuthService;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Sets Firebase custom claim role for a user (admin, vendor, planner, user).
    /// Requires Development environment, or header X-Bootstrap-Secret matching AdminBootstrap:Secret.
    /// </summary>
    [HttpPost("set-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRole(
        [FromBody] SetRoleRequest request,
        [FromHeader(Name = "X-Bootstrap-Secret")] string? bootstrapSecret,
        CancellationToken cancellationToken)
    {
        if (!IsBootstrapAllowed(bootstrapSecret))
        {
            return NotFound();
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

    private bool IsBootstrapAllowed(string? bootstrapSecret)
    {
        if (_environment.IsDevelopment())
            return true;

        var expectedSecret = _configuration["AdminBootstrap:Secret"];
        return !string.IsNullOrWhiteSpace(expectedSecret) &&
               string.Equals(bootstrapSecret, expectedSecret, StringComparison.Ordinal);
    }
}

public class SetRoleRequest
{
    public required string UserId { get; set; }
    public required string Role { get; set; }
}
