using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MyWedding.API.Auth.Testing;

/// <summary>
/// Test-only authentication. Use header: Authorization: Test {userId}|{role}
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
            return Task.FromResult(AuthenticateResult.NoResult());

        var value = authorization.ToString();
        if (!value.StartsWith($"{SchemeName} ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var payload = value[(SchemeName.Length + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(payload))
            return Task.FromResult(AuthenticateResult.Fail("Missing test user payload."));

        var segments = payload.Split('|', 2);
        var userId = segments[0].Trim();
        var role = segments.Length > 1 ? segments[1].Trim() : "user";

        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.Fail("Missing test user id."));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("user_id", userId),
            new("firebase_uid", userId),
            new("role", role),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
