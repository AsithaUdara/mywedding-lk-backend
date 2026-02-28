using System;

namespace MyWedding.Domain.Entities
{
    public class EventInvitation
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public WeddingEvent? WeddingEvent { get; set; }
        
        public required string Email { get; set; }
        public required string Token { get; set; }
        
        public DateTime InvitedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime? AcceptedAt { get; set; }
        
        public required string InvitedById { get; set; }
        public User? InvitedBy { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && !IsAccepted;
    }
}
