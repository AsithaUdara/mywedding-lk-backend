using Microsoft.AspNetCore.SignalR;

using MyWedding.API.Hubs;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Internal service — not part of OpenAPI

namespace MyWedding.API.Services
{
    /// <summary>
    /// Implementation of ICollaborationService using the strongly-typed SignalR IHubContext.
    /// Broadcasts signals to event-specific SignalR groups via ICollaborationHubClient.
    /// </summary>
    public class CollaborationService : ICollaborationService
    {
        private readonly IHubContext<CollaborationHub, ICollaborationHubClient> _hubContext;

        public CollaborationService(IHubContext<CollaborationHub, ICollaborationHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyMessageAsync(Guid eventId, object message)
        {
            await _hubContext.Clients.Group(eventId.ToString()).ReceiveMessage(message);
        }

        public async Task NotifyActivityAsync(Guid eventId, object activity)
        {
            await _hubContext.Clients.Group(eventId.ToString()).ReceiveActivity(activity);
        }

        public async Task NotifyChecklistUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).ChecklistUpdated();
        }

        public async Task NotifyBudgetUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).BudgetUpdated();
        }

        public async Task NotifyPollsUpdatedAsync(Guid eventId)
        {
            await _hubContext.Clients.Group(eventId.ToString()).PollsUpdated();
        }

        public async Task NotifyInvitationAcceptedAsync(Guid eventId, string email)
        {
            await _hubContext.Clients.Group(eventId.ToString()).InvitationAccepted(new { email });
        }
    }
}
