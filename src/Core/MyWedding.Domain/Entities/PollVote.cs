// File: src/Core/MyWedding.Domain/Entities/PollVote.cs
using System;

namespace MyWedding.Domain.Entities
{
    public class PollVote
    {
        public Guid Id { get; set; }
        
        // Foreign Keys
        public Guid PollOptionId { get; set; }
        public PollOption? PollOption { get; set; }

        public required string UserId { get; set; }
        public User? User { get; set; }

        public DateTime VotedAt { get; set; }
    }
}
