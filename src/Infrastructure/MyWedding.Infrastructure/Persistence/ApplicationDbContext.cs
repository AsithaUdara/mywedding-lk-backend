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
        }
    }
}