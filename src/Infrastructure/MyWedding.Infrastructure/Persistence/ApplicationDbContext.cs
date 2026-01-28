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
        }
    }
}