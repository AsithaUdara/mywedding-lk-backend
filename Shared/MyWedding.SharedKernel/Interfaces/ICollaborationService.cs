using System;
using System.Threading.Tasks;

namespace MyWedding.SharedKernel.Interfaces
{
    /// <summary>
    /// Service for orchestrating real-time collaboration signals across the application.
    /// Acts as an abstraction for SignalR Hubs to avoid direct coupling with the Application layer.
    /// </summary>
    public interface ICollaborationService
    {
        /// <summary>
        /// Broadcasts a new message to all members in an event group.
        /// </summary>
        Task NotifyMessageAsync(Guid eventId, object message);

        /// <summary>
        /// Broadcasts an activity feed update (system log or comment) to the event group.
        /// </summary>
        Task NotifyActivityAsync(Guid eventId, object activity);

        /// <summary>
        /// Signals that the checklist has changed (task added, status updated).
        /// </summary>
        Task NotifyChecklistUpdatedAsync(Guid eventId);

        /// <summary>
        /// Signals that the budget or expenses have changed.
        /// </summary>
        Task NotifyBudgetUpdatedAsync(Guid eventId);

        /// <summary>
        /// Signals that a poll was created or a vote was cast.
        /// </summary>
        Task NotifyPollsUpdatedAsync(Guid eventId);

        /// <summary>
        /// Signals that an invitation has been accepted.
        /// </summary>
        Task NotifyInvitationAcceptedAsync(Guid eventId, string email);
    }
}
