// File: src/Core/MyWedding.Domain/Entities/Message.cs
using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign Key to the Conversation this message belongs to
        public Guid ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        // Foreign Key to the User who sent the message
        public required string SenderId { get; set; }
        public User? Sender { get; set; }

        // --- "Smart Attachment" Foreign Keys (Nullable) ---
        // These allow a message to be optionally linked to another entity.
        // Only one of these should be set for a given message.
        public Guid? AttachedVendorServiceId { get; set; }
        public VendorService? AttachedVendorService { get; set; }

        public Guid? AttachedEventTaskId { get; set; }
        public EventTask? AttachedEventTask { get; set; }

        public Guid? AttachedExpenseId { get; set; }
        public Expense? AttachedExpense { get; set; }
        
        // Navigation property for read receipts
        public ICollection<MessageReadStatus> ReadStatuses { get; set; } = new List<MessageReadStatus>();
    }
}
