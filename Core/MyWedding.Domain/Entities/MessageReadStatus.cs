// File: src/Core/MyWedding.Domain/Entities/MessageReadStatus.cs
using System;

namespace MyWedding.Domain.Entities
{
    public class MessageReadStatus
    {
        // Composite Primary Key - Part 1: Foreign Key to Message
        public Guid MessageId { get; set; }
        public Message? Message { get; set; }

        // Composite Primary Key - Part 2: Foreign Key to User
        public required string UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime ReadAt { get; set; }
    }
}
