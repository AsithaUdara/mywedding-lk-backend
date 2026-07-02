namespace MyWedding.SharedKernel.Interfaces;

/// <summary>
/// Transactional email delivery (invitations, booking updates, etc.).
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an event team invitation with accept link.</summary>
    Task SendEventInvitationAsync(EventInvitationEmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>Notifies the client that a vendor accepted their booking request.</summary>
    Task SendVendorBookingAcceptedAsync(
        VendorBookingAcceptedEmailMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>Notifies a vendor that their marketplace application was not approved.</summary>
    Task SendVendorRejectionAsync(
        string toEmail,
        string businessName,
        CancellationToken cancellationToken = default);
}

public sealed record EventInvitationEmailMessage(
    string ToEmail,
    string EventName,
    Guid EventId,
    string InvitationToken,
    string Role,
    string PermissionLevel);

public sealed record VendorBookingAcceptedEmailMessage(
    string ToEmail,
    string EventName,
    string VendorBusinessName,
    Guid BookingId,
    Guid EventId);
