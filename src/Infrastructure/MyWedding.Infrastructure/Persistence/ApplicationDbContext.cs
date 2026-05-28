using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        private readonly ICurrentPlannerAccessor _currentPlannerAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentPlannerAccessor currentPlannerAccessor) : base(options)
        {
            _currentPlannerAccessor = currentPlannerAccessor;
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
        public DbSet<VendorShortlistItem> VendorShortlistItems { get; set; }
        public DbSet<BookingContract> BookingContracts { get; set; }
        public DbSet<VendorInquiry> VendorInquiries { get; set; }
        public DbSet<VendorInquiryQuote> VendorInquiryQuotes { get; set; }
        public DbSet<VendorBlockedDate> VendorBlockedDates { get; set; }
        public DbSet<VendorProfileView> VendorProfileViews { get; set; }
        public DbSet<WeddingPlanner> WeddingPlanners { get; set; }
        public DbSet<PlannerClientEvent> PlannerClientEvents { get; set; }
        public DbSet<BookingPaymentTransaction> BookingPaymentTransactions { get; set; }
        public DbSet<CommissionSettlement> CommissionSettlements { get; set; }
        public DbSet<VendorSubscription> VendorSubscriptions { get; set; }
        public DbSet<VendorBillingProfile> VendorBillingProfiles { get; set; }
        public DbSet<VendorSubscriptionCheckout> VendorSubscriptionCheckouts { get; set; }
        public DbSet<PlannerSubscription> PlannerSubscriptions { get; set; }
        public DbSet<EventItinerary> EventItineraries { get; set; }
        public DbSet<EventItineraryItem> EventItineraryItems { get; set; }
        
        public DbSet<AuditLogItem> AuditLogItems { get; set; }
        
        // Collaboration Hub DbSets
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReadStatus> MessageReadStatuses { get; set; }
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

            modelBuilder.Entity<WeddingEvent>()
                .HasOne(e => e.ManagingPlanner)
                .WithMany()
                .HasForeignKey(e => e.ManagingPlannerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WeddingEvent>()
                .Property(e => e.EventLifecycleStage)
                .HasConversion<int>();

            modelBuilder.Entity<WeddingEvent>()
                .HasQueryFilter(e =>
                    !_currentPlannerAccessor.IsPlanner ||
                    (e.ManagingPlannerId != null && e.ManagingPlannerId == _currentPlannerAccessor.PlannerId));

            // EventTask -> WeddingEvent (cascade delete tasks when event is deleted)
            modelBuilder.Entity<EventTask>()
                .HasOne(t => t.WeddingEvent)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventTask>()
                .HasOne(t => t.DependsOnTask)
                .WithMany(t => t.DependentTasks)
                .HasForeignKey(t => t.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventTask>()
                .HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<VendorInquiry>()
                .HasOne(vi => vi.WeddingEvent)
                .WithMany()
                .HasForeignKey(vi => vi.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VendorInquiryQuote>()
                .HasOne(q => q.Inquiry)
                .WithMany()
                .HasForeignKey(q => q.InquiryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorBlockedDate>()
                .HasOne(b => b.Vendor)
                .WithMany()
                .HasForeignKey(b => b.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorBlockedDate>()
                .HasIndex(b => new { b.VendorId, b.Date })
                .IsUnique();

            modelBuilder.Entity<VendorProfileView>()
                .HasOne(v => v.Vendor)
                .WithMany()
                .HasForeignKey(v => v.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorProfileView>()
                .HasIndex(v => new { v.VendorId, v.ViewedAt });

            // VendorReview -> WeddingEvent (Many-to-One, cascade delete reviews when event is deleted)
            modelBuilder.Entity<VendorReview>()
                .HasOne(vr => vr.WeddingEvent)
                .WithMany()
                .HasForeignKey(vr => vr.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== PLANNER RELATIONSHIPS ==========
            modelBuilder.Entity<WeddingPlanner>()
                .HasKey(p => p.UserId);

            modelBuilder.Entity<WeddingPlanner>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<WeddingPlanner>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlannerClientEvent>()
                .HasOne(pce => pce.Planner)
                .WithMany(p => p.ClientEvents)
                .HasForeignKey(pce => pce.PlannerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlannerClientEvent>()
                .HasOne(pce => pce.WeddingEvent)
                .WithMany()
                .HasForeignKey(pce => pce.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlannerClientEvent>()
                .HasOne(pce => pce.ClientUser)
                .WithMany()
                .HasForeignKey(pce => pce.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlannerClientEvent>()
                .HasIndex(pce => new { pce.PlannerId, pce.EventId })
                .IsUnique();

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

            modelBuilder.Entity<VendorShortlistItem>()
                .HasOne(i => i.WeddingEvent)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VendorShortlistItem>()
                .HasOne(i => i.VendorService)
                .WithMany()
                .HasForeignKey(i => i.VendorServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VendorShortlistItem>()
                .HasOne(i => i.VendorBooking)
                .WithMany()
                .HasForeignKey(i => i.VendorBookingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VendorShortlistItem>()
                .Property(i => i.Status)
                .HasConversion<int>();

            modelBuilder.Entity<VendorShortlistItem>()
                .HasIndex(i => new { i.EventId, i.VendorServiceId })
                .IsUnique();

            modelBuilder.Entity<BookingPaymentTransaction>()
                .HasOne(t => t.Booking)
                .WithMany()
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingPaymentTransaction>()
                .HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<CommissionSettlement>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommissionSettlement>()
                .HasIndex(c => c.BookingId)
                .IsUnique();

            modelBuilder.Entity<VendorSubscription>()
                .HasOne(s => s.Vendor)
                .WithMany()
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorBillingProfile>()
                .HasKey(p => p.VendorId);

            modelBuilder.Entity<VendorBillingProfile>()
                .HasOne(p => p.Vendor)
                .WithOne()
                .HasForeignKey<VendorBillingProfile>(p => p.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorSubscriptionCheckout>()
                .HasOne(c => c.Vendor)
                .WithMany()
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlannerSubscription>()
                .HasOne(s => s.Planner)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlannerId)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<BookingPaymentTransaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CommissionSettlement>()
                .Property(c => c.GrossAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CommissionSettlement>()
                .Property(c => c.CommissionAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CommissionSettlement>()
                .Property(c => c.VendorNetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CommissionSettlement>()
                .Property(c => c.CommissionRate)
                .HasColumnType("decimal(5,4)");

            modelBuilder.Entity<VendorSubscription>()
                .Property(s => s.MonthlyFee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<VendorSubscriptionCheckout>()
                .Property(c => c.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PlannerSubscription>()
                .Property(s => s.MonthlyFee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<AuditLogItem>()
                .HasOne(i => i.WeddingEvent)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditLogItem>()
                .HasOne(i => i.Actor)
                .WithMany()
                .HasForeignKey(i => i.ActorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditLogItem>()
                .ToTable("AuditLogItems");

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

            modelBuilder.Entity<EventItinerary>()
                .HasOne(i => i.WeddingEvent)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventItineraryItem>()
                .HasOne(i => i.Itinerary)
                .WithMany(i => i.Items)
                .HasForeignKey(i => i.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}