namespace MyWedding.SharedKernel.Interfaces;

public interface INotificationService
{
    Task NotifyBookingApprovedAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default);
    Task NotifyBookingConfirmedAsync(string clientUserId, object payload, CancellationToken cancellationToken = default);
    Task NotifyBookingConfirmedForPlannerAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default);
    Task NotifyNewInquiryAsync(string vendorUserId, object payload, CancellationToken cancellationToken = default);
    Task NotifyProposalReceivedAsync(string clientUserId, object payload, CancellationToken cancellationToken = default);
    Task NotifyContractSignedAsync(IEnumerable<string> userIds, object payload, CancellationToken cancellationToken = default);
    Task NotifyVendorBookingDeclinedForPlannerAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default);
}
