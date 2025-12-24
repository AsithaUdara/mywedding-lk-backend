using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult GetPublicData()
    {
        return Ok("This is public data. Anyone can see this.");
    }

    [HttpGet("secure")]
    [Authorize] // <-- This attribute PROTECTS the endpoint
    public IActionResult GetSecureData()
    {
        // We can get the user's Firebase UID from the token claims
        var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok($"This is secure data. Hello, user with Firebase UID: {firebaseUid}");
    }
}