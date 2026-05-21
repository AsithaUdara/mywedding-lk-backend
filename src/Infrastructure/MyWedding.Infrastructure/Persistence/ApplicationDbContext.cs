using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<WeddingEvent> WeddingEvents { get; set; }
        public DbSet<EventOrganizer> EventOrganizers { get; set; }
        public DbSet<EventTask> EventTasks { get; set; }
        public DbSet<BudgetCategory> BudgetCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        
        // Vendor Management DbSets
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<VendorCategory> VendorCategories { get; set; }
        public DbSet<VendorService> VendorServices { get; set; }
        public DbSet<VendorReview> VendorReviews { get; set; }
        public DbSet<VendorBooking> VendorBookings { get; set; }
        public DbSet<BookingContract> BookingContracts { get; set; }
        public DbSet<VendorInquiry> VendorInquiries { get; set; }
        
        // Activity Feed DbSet
        public DbSet<ActivityFeedItem> ActivityFeedItems { get; set; }
        
        // Collaboration Hub DbSets
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReadStatus> MessageReadStatuses { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<EventInvitation> EventInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for EventOrganizer
            modelBuilder.Entity<EventOrganizer>()
                .HasKey(eo => new { eo.EventId, eo.UserId });

            // EventOrganizer -> WeddingEvent (cascade delete organizers when event is deleted)
            modelBuilder.Entity<EventOrganizer>()
                .HasOne(eo => eo.WeddingEvent)
                .WithMany()
                .HasForeignKey(eo => eo.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // EventOrganizer -> User (cascade delete organizers when user is deleted)
            modelBuilder.Entity<EventOrganizer>()
                .HasOne(eo => eo.User)
                .WithMany()
                .HasForeignKey(eo => eo.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WeddingEvent -> User (CreatedBy) - restrict delete to avoid multiple cascade paths
            modelBuilder.Entity<WeddingEvent>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // EventTask -> WeddingEvent (cascade delete tasks when event is deleted)
            modelBuilder.Entity<EventTask>()
                .HasOne(t => t.WeddingEvent)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Expense -> WeddingEvent (cascade delete expenses when event is deleted)
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.WeddingEvent)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Expense -> BudgetCategory (restrict delete when referenced)
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.BudgetCategory)
                .WithMany()
                .HasForeignKey(e => e.BudgetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== VENDOR MANAGEMENT RELATIONSHIPS ==========

            // Vendor: Primary key is UserId (1-to-1 with User)
            modelBuilder.Entity<Vendor>()
                .HasKey(v => v.UserId);

            // Vendor -> User (1-to-1, cascade delete vendor when user is deleted)
            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.User)
                .WithOne()
                .HasForeignKey<Vendor>(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vendor -> PrimaryCategory (Many-to-One, restrict delete)
            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.PrimaryCategory)
                .WithMany()
                .HasForeignKey(v => v.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // VendorService -> Vendor (Many-to-One, cascade delete services when vendor is deleted)
            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.Vendor)
                .WithMany(v => v.Services)
                .HasForeignKey(vs => vs.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            // VendorService -> VendorCategory (Many-to-One, restrict delete when referenced)
            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.Category)
                .WithMany()
                .HasForeignKey(vs => vs.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // VendorReview -> Vendor (Many-to-One, cascade delete reviews when vendor is deleted)
            modelBuilder.Entity<VendorReview>()
                .HasOne(vr => vr.Vendor)
                .WithMany(v => v.Reviews)
                .HasForeignKey(vr => vr.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            // VendorReview -> User/Reviewer (Many-to-One, restrict delete when referenced)
            modelBuilder.Entity<VendorReview>()
                .HasOne(vr => vr.Reviewer)
                .WithMany()
                .HasForeignKey(vr => vr.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // VendorInquiry -> Vendor (Many-to-One, restrict delete)
            modelBuilder.Entity<VendorInquiry>()
                .HasOne(vi => vi.Vendor)
                .WithMany(v => v.Inquiries)
                .HasForeignKey(vi => vi.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            // VendorReview -> WeddingEvent (Many-to-One, cascade delete reviews when event is deleted)
            modelBuilder.Entity<VendorReview>()
                .HasOne(vr => vr.WeddingEvent)
                .WithMany()
                .HasForeignKey(vr => vr.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== BOOKING & CONTRACT RELATIONSHIPS ==========

            // VendorBooking -> WeddingEvent (Many-to-One)
            modelBuilder.Entity<VendorBooking>()
                .HasOne(b => b.WeddingEvent)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // VendorBooking -> VendorService (Many-to-One, restrict delete when referenced)
            modelBuilder.Entity<VendorBooking>()
                .HasOne(b => b.VendorService)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // VendorBooking -> User (BookedBy, Many-to-One, restrict delete)
            modelBuilder.Entity<VendorBooking>()
                .HasOne(b => b.BookedBy)
                .WithMany()
                .HasForeignKey(b => b.BookedById)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-one: VendorBooking <-> BookingContract (PK of contract is FK to booking, contract optional at creation)
            modelBuilder.Entity<VendorBooking>()
                .HasOne(b => b.BookingContract)
                .WithOne(c => c.VendorBooking)
                .HasForeignKey<BookingContract>(c => c.Id)
                .IsRequired(false);

            // ========== DECIMAL PRECISION CONFIGURATION ==========

            // Financial values
            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WeddingEvent>()
                .Property(e => e.TotalBudget)
                .HasColumnType("decimal(18,2)");

            // Vendor financial values
            modelBuilder.Entity<Vendor>()
                .Property(v => v.AverageRating)
                .HasColumnType("decimal(3,2)");

            modelBuilder.Entity<VendorService>()
                .Property(vs => vs.BasePrice)
                .HasColumnType("decimal(18,2)");

            // Booking financial values
            modelBuilder.Entity<VendorBooking>()
                .Property(b => b.FinalAmount)
                .HasColumnType("decimal(18,2)");

            // --- NEW CONFIGURATION FOR ACTIVITYFEEDITEM ---
            modelBuilder.Entity<ActivityFeedItem>()
                .HasOne(i => i.WeddingEvent)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActivityFeedItem>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- NEW CONFIGURATION FOR COLLABORATION HUB ---
            
            // Conversation -> WeddingEvent relationship
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.WeddingEvent)
                .WithMany() // An event can have many conversations
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message -> Conversation relationship
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages) // A conversation has a collection of messages
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message -> User (Sender) relationship
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany() // A user can send many messages
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // "Smart Attachment" relationships (all optional)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.AttachedVendorService)
                .WithMany()
                .HasForeignKey(m => m.AttachedVendorServiceId)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<Message>()
                .HasOne(m => m.AttachedEventTask)
                .WithMany()
                .HasForeignKey(m => m.AttachedEventTaskId)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<Message>()
                .HasOne(m => m.AttachedExpense)
                .WithMany()
                .HasForeignKey(m => m.AttachedExpenseId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // MessageReadStatus composite key and relationships
            modelBuilder.Entity<MessageReadStatus>()
                .HasKey(rs => new { rs.MessageId, rs.UserId });
                
            modelBuilder.Entity<MessageReadStatus>()
                .HasOne(rs => rs.Message)
                .WithMany(m => m.ReadStatuses)
                .HasForeignKey(rs => rs.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<MessageReadStatus>()
                .HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevents cycles

            // Polls Configuration
            modelBuilder.Entity<Poll>()
                .HasOne(p => p.WeddingEvent)
                .WithMany()
                .HasForeignKey(p => p.EventId);

            modelBuilder.Entity<PollOption>()
                .HasOne(po => po.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(po => po.PollId);

            modelBuilder.Entity<PollVote>()
                .HasOne(pv => pv.PollOption)
                .WithMany(po => po.Votes)
                .HasForeignKey(pv => pv.PollOptionId);

            modelBuilder.Entity<PollVote>()
                .HasOne(pv => pv.User)
                .WithMany()
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Fix for multiple cascade paths in SQL Server

            // EventInvitation Configuration
            modelBuilder.Entity<EventInvitation>()
                .HasOne(i => i.WeddingEvent)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventInvitation>()
                .HasOne(i => i.InvitedBy)
                .WithMany()
                .HasForeignKey(i => i.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventInvitation>()
                .HasIndex(i => i.Token)
                .IsUnique();
        }
    }
}