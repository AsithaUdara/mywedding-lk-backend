using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _frontendBaseUrl;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
    }

    public async Task SendEventInvitationAsync(
        EventInvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var acceptUrl =
            $"{_frontendBaseUrl}/invitations/accept?token={Uri.EscapeDataString(message.InvitationToken)}";

        var plainText = $"""
            You have been invited to join "{message.EventName}" on MyWedding.lk as {message.Role} ({message.PermissionLevel} access).

            Accept your invitation:
            {acceptUrl}

            This link expires in 7 days.
            """;

        var html = $"""
            <p>You have been invited to join <strong>{message.EventName}</strong> on MyWedding.lk as <strong>{message.Role}</strong> ({message.PermissionLevel} access).</p>
            <p><a href="{acceptUrl}">Accept your invitation</a></p>
            <p><small>This link expires in 7 days.</small></p>
            """;

        await SendAsync(
            message.ToEmail,
            $"You're invited to {message.EventName} on MyWedding.lk",
            plainText,
            html,
            cancellationToken);
    }

    public async Task SendVendorBookingAcceptedAsync(
        VendorBookingAcceptedEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var vendorsUrl = $"{_frontendBaseUrl}/events/{message.EventId}/vendors";

        var plainText = $"""
            Great news — {message.VendorBusinessName} accepted your booking request for "{message.EventName}".

            Booking reference: {message.BookingId}
            Next step: pay the deposit to confirm your vendor.
            {vendorsUrl}
            """;

        var html = $"""
            <p>Great news — <strong>{message.VendorBusinessName}</strong> accepted your booking request for <strong>{message.EventName}</strong>.</p>
            <p>Booking reference: <code>{message.BookingId}</code></p>
            <p><a href="{vendorsUrl}">Pay your deposit and confirm the vendor</a></p>
            """;

        await SendAsync(
            message.ToEmail,
            $"{message.VendorBusinessName} accepted your booking",
            plainText,
            html,
            cancellationToken);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string plainText,
        string html,
        CancellationToken cancellationToken)
    {
        var fromEmail = _configuration["Smtp:FromEmail"]
            ?? _configuration["Smtp:Username"]
            ?? throw new InvalidOperationException("Smtp:FromEmail or Smtp:Username is required.");
        var fromName = _configuration["Smtp:FromName"] ?? "MyWedding.lk";
        var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var username = _configuration["Smtp:Username"]
            ?? throw new InvalidOperationException("Smtp:Username is required.");
        var password = _configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("Smtp:Password is required (use a Gmail App Password, not your login password).");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            TextBody = plainText,
            HtmlBody = html
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("SMTP email sent to {ToEmail} subject={Subject}", toEmail, subject);
    }
}
