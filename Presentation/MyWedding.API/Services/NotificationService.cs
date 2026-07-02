using Microsoft.AspNetCore.SignalR;
using MyWedding.API.Hubs;
using MyWedding.SharedKernel.Interfaces;

#pragma warning disable CS1591 // Internal service — not part of OpenAPI

namespace MyWedding.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub, INotificationHubClient> _hubContext;

    public NotificationService(IHubContext<NotificationHub, INotificationHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyBookingApprovedAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Groups(NotificationHub.UserGroup(plannerUserId), NotificationHub.PlannerGroup(plannerUserId))
            .NotifyBookingApproved(payload);
    }

    public Task NotifyBookingConfirmedAsync(string clientUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(NotificationHub.UserGroup(clientUserId))
            .NotifyBookingConfirmed(payload);
    }

    public Task NotifyBookingConfirmedForPlannerAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Groups(NotificationHub.UserGroup(plannerUserId), NotificationHub.PlannerGroup(plannerUserId))
            .NotifyBookingConfirmed(payload);
    }

    public Task NotifyNewInquiryAsync(string vendorUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Groups(NotificationHub.UserGroup(vendorUserId), NotificationHub.VendorGroup(vendorUserId))
            .NotifyNewInquiry(payload);
    }

    public Task NotifyProposalReceivedAsync(string clientUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(NotificationHub.UserGroup(clientUserId))
            .NotifyProposalReceived(payload);
    }

    public Task NotifyVendorBookingDeclinedForPlannerAsync(string plannerUserId, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Groups(NotificationHub.UserGroup(plannerUserId), NotificationHub.PlannerGroup(plannerUserId))
            .NotifyVendorBookingDeclined(payload);
    }

    public async Task NotifyContractSignedAsync(IEnumerable<string> userIds, object payload, CancellationToken cancellationToken = default)
    {
        var groups = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(NotificationHub.UserGroup)
            .Distinct()
            .ToList();

        if (groups.Count == 0)
        {
            return;
        }

        await _hubContext.Clients.Groups(groups).NotifyContractSigned(payload);
    }
}
