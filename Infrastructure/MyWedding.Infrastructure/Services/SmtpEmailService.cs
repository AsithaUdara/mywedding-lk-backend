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
            $"{_frontendBaseUrl}/invite/accept?token={Uri.EscapeDataString(message.InvitationToken)}";

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

    public async Task SendVendorRejectionAsync(
        string toEmail,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        var directoryUrl = $"{_frontendBaseUrl}/vendors";

        var plainText = $"""
            Thank you for applying to list {businessName} on MyWedding.lk.

            After reviewing your submission, we are unable to approve your vendor profile at this time. This may be due to incomplete business details, missing verification documents, or information that does not meet our marketplace guidelines.

            You may update your profile and contact our team if you believe this decision was made in error.

            Browse the marketplace: {directoryUrl}
            """;

        var html = $"""
            <p>Thank you for applying to list <strong>{businessName}</strong> on MyWedding.lk.</p>
            <p>After reviewing your submission, we are unable to approve your vendor profile at this time. This may be due to incomplete business details, missing verification documents, or information that does not meet our marketplace guidelines.</p>
            <p>You may update your profile and contact our team if you believe this decision was made in error.</p>
            <p><a href="{directoryUrl}">Visit MyWedding.lk</a></p>
            """;

        await SendAsync(
            toEmail,
            "Update on your MyWedding.lk vendor application",
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

        // Windows dev machines often fail OCSP/revocation checks against Gmail SMTP.
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            client.CheckCertificateRevocation = false;
        }

        var secureSocketOptions = ResolveSecureSocketOptions(port);

        try
        {
            await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (SslHandshakeException ex)
        {
            _logger.LogError(
                ex,
                "SMTP SSL handshake failed for {Host}:{Port} ({Security}). " +
                "For Gmail use port 587 + StartTls, or port 465 + SslOnConnect with an App Password.",
                host,
                port,
                secureSocketOptions);
            throw;
        }

        _logger.LogInformation("SMTP email sent to {ToEmail} subject={Subject}", toEmail, subject);
    }

    private SecureSocketOptions ResolveSecureSocketOptions(int port)
    {
        var configured = _configuration["Smtp:Security"]?.Trim();
        if (!string.IsNullOrEmpty(configured))
        {
            return configured.ToLowerInvariant() switch
            {
                "ssl" or "sslonconnect" => SecureSocketOptions.SslOnConnect,
                "starttls" or "tls" => SecureSocketOptions.StartTls,
                "auto" => SecureSocketOptions.Auto,
                "none" => SecureSocketOptions.None,
                _ => SecureSocketOptions.StartTls
            };
        }

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
