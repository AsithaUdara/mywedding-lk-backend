// File: src/Core/MyWedding.Domain/Entities/PollOption.cs
using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class PollOption
    {
        public Guid Id { get; set; }
        public required string OptionText { get; set; }

        // Foreign Keys
        public Guid PollId { get; set; }
        public Poll? Poll { get; set; }

        // Navigation Properties
        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
