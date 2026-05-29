using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MyWedding.API.Hubs;

/// <summary>
/// Platform-wide notification hub for planner, vendor, and client dashboards.
/// </summary>
public interface INotificationHubClient
{
    Task NotifyBookingApproved(object payload);
    Task NotifyBookingConfirmed(object payload);
    Task NotifyNewInquiry(object payload);
    Task NotifyProposalReceived(object payload);
    Task NotifyContractSigned(object payload);
    Task NotifyVendorBookingDeclined(object payload);
}

[Authorize]
public class NotificationHub : Hub<INotificationHubClient>
{
    public static string UserGroup(string userId) => $"user:{userId}";
    public static string PlannerGroup(string plannerId) => $"planner:{plannerId}";
    public static string VendorGroup(string vendorId) => $"vendor:{vendorId}";

    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            var role = Context.User?.FindFirstValue("role")
                ?? Context.User?.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(role, "planner", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, PlannerGroup(userId));
            }
            else if (string.Equals(role, "vendor", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, VendorGroup(userId));
            }
        }

        _logger.LogInformation("Notification connection {ConnectionId} joined groups for user {UserId}.",
            Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Notification connection {ConnectionId} disconnected.", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
