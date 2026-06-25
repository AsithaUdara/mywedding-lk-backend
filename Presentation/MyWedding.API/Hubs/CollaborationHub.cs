using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyWedding.Domain.Interfaces;
using System.Security.Claims;

namespace MyWedding.API.Hubs
{
    public interface ICollaborationHubClient
    {
        Task ReceiveMessage(object message);
        Task ReceiveActivity(object activity);
        Task ChecklistUpdated();
        Task PollsUpdated();
        Task BudgetUpdated();
        Task InvitationAccepted(object data);
    }

    [Authorize]
    public class CollaborationHub : Hub<ICollaborationHubClient>
    {
        private readonly ILogger<CollaborationHub> _logger;
        private readonly IEventOrganizerRepository _organizerRepository;

        public CollaborationHub(
            ILogger<CollaborationHub> logger,
            IEventOrganizerRepository organizerRepository)
        {
            _logger = logger;
            _organizerRepository = organizerRepository;
        }

        public async Task JoinEventGroup(Guid eventId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Authentication required.");
            }

            var isMember = await _organizerRepository.IsUserAlreadyOrganizerAsync(eventId, userId);
            if (!isMember)
            {
                _logger.LogWarning(
                    "User {UserId} denied join to event group {EventId}.",
                    userId,
                    eventId);
                throw new HubException("You do not have access to this event.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, eventId.ToString());
            _logger.LogInformation(
                "Connection {ConnectionId} joined event group {EventId}.",
                Context.ConnectionId,
                eventId);
        }

        public async Task LeaveEventGroup(Guid eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId.ToString());
            _logger.LogInformation(
                "Connection {ConnectionId} left event group {EventId}.",
                Context.ConnectionId,
                eventId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Connection {ConnectionId} disconnected.", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
