using Microsoft.AspNetCore.SignalR;

using MyWedding.API.Hubs;
using System;
using System.Threading.Tasks;

namespace MyWedding.API.Services
{
    /// <summary>
    /// Implementation of ICollaborationService using SignalR IHubContext.
    /// Broadcasts signals to event-specific SignalR groups.
    /// </summary>
    public class CollaborationService : ICollaborationService
    {
        private readonly IHubContext<CollaborationHub> _hubContext;

        public CollaborationService(IHubContext<CollaborationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyMessageAsync(Guid eventId, object message)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task NotifyActivityAsync(Guid eventId, object activity)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ReceiveActivity", activity);
        }

        public async Task NotifyChecklistUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ChecklistUpdated");
        }

        public async Task NotifyBudgetUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("BudgetUpdated");
        }

        public async Task NotifyPollsUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("PollsUpdated");
        }

        public async Task NotifyInvitationAcceptedAsync(Guid eventId, string email)
        {
            await _hubContext.Clients.Group(eventId.ToString()).SendAsync("InvitationAccepted", new { email });
        }
    }
}
