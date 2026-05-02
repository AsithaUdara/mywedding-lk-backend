using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace MyWedding.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time wedding event collaboration.
    /// Manages user grouping by EventId to allow isolated broadcasts.
    /// </summary>
    public class CollaborationHub : Hub
    {
        private readonly ILogger<CollaborationHub> _logger;

        public CollaborationHub(ILogger<CollaborationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Allows a user to join an event's real-time group for notifications.
        /// </summary>
        /// <param name="eventId">The ID of the wedding event group to join.</param>
        public async Task JoinEventGroup(Guid eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, eventId.ToString());
            _logger.LogInformation("Connection {ConnectionId} joined event group {EventId}.", Context.ConnectionId, eventId);
        }

        /// <summary>
        /// Allows a user to leave an event's real-time group.
        /// </summary>
        /// <param name="eventId">The ID of the wedding event group to leave.</param>
        public async Task LeaveEventGroup(Guid eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId.ToString());
            _logger.LogInformation("Connection {ConnectionId} left event group {EventId}.", Context.ConnectionId, eventId);
        }

        /// <summary>
        /// Fired when a connection is closed.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Connection {ConnectionId} disconnected.", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
