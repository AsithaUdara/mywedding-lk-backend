using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services.Mocks;

/// <summary>
/// Logs simulated transactional emails until a real provider is configured.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    private readonly string _frontendBaseUrl;

    public MockEmailService(IConfiguration configuration, ILogger<MockEmailService> logger)
    {
        _logger = logger;
        _frontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
    }

    public Task SendEventInvitationAsync(
        EventInvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Dev-only: configure Smtp:* user secrets for real delivery (see docs/REAL_API_SETUP.md).
        var acceptUrl = $"{_frontendBaseUrl}/invite/accept?token={Uri.EscapeDataString(message.InvitationToken)}";
        var body = $"""
            You have been invited to join "{message.EventName}" on MyWedding.lk as {message.Role} ({message.PermissionLevel} access).

            Accept your invitation:
            {acceptUrl}

            This link expires in 7 days.
            """;

        LogSimulatedEmail("Event invitation", message.ToEmail, $"Join {message.EventName} on MyWedding.lk", body);
        return Task.CompletedTask;
    }

    public Task SendVendorBookingAcceptedAsync(
        VendorBookingAcceptedEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Dev-only: configure Smtp:* user secrets for real delivery (see docs/REAL_API_SETUP.md).
        var vendorsUrl = $"{_frontendBaseUrl}/events/{message.EventId}/vendors";
        var body = $"""
            Great news — {message.VendorBusinessName} accepted your booking request for "{message.EventName}".

            Booking reference: {message.BookingId}
            Next step: pay the deposit to confirm your vendor.
            {vendorsUrl}
            """;

        LogSimulatedEmail(
            "Vendor booking accepted",
            message.ToEmail,
            $"{message.VendorBusinessName} accepted your booking",
            body);
        return Task.CompletedTask;
    }

    public Task SendVendorRejectionAsync(
        string toEmail,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = $"""
            Thank you for applying to list "{businessName}" on MyWedding.lk.

            After review, we are unable to approve your vendor profile at this time.
            """;

        LogSimulatedEmail(
            "Vendor KYB rejection",
            toEmail,
            "Update on your MyWedding.lk vendor application",
            body);
        return Task.CompletedTask;
    }

    private void LogSimulatedEmail(string category, string to, string subject, string body)
    {
        var banner = $"[MockEmail] {category}";
        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
        Console.WriteLine(banner);
        Console.WriteLine($"To:      {to}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine(body);
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();

        _logger.LogInformation(
            "{Banner} To={ToEmail} Subject={Subject}",
            banner,
            to,
            subject);
    }
}
