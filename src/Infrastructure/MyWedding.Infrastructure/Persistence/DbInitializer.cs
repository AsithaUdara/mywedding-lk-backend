// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/DbInitializer.cs
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            if (!context.VendorCategories.Any())
            {
                var categories = new[]
                {
                    new VendorCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Venue", Description = "Hotels, Estates, Beachfronts" },
                    new VendorCategory { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Photography", Description = "Capture precious moments" },
                    new VendorCategory { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Catering", Description = "Exquisite food & service" },
                    new VendorCategory { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Floral & Decor", Description = "Stunning arrangements" },
                    new VendorCategory { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Music & DJ", Description = "Unforgettable entertainment" },
                    new VendorCategory { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Other", Description = "Planning, Cakes, Rentals etc" }
                };

                await context.VendorCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // Seed default conversation channels for events that don't have any
            var eventsWithoutConversations = await context.WeddingEvents
                .Where(e => !context.Conversations.Any(c => c.EventId == e.Id))
                .ToListAsync();

            if (eventsWithoutConversations.Any())
            {
                foreach (var weddingEvent in eventsWithoutConversations)
                {
                    context.Conversations.AddRange(
                        new Conversation
                        {
                            Id = Guid.NewGuid(),
                            Name = "general",
                            EventId = weddingEvent.Id,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Conversation
                        {
                            Id = Guid.NewGuid(),
                            Name = "planning",
                            EventId = weddingEvent.Id,
                            CreatedAt = DateTime.UtcNow
                        }
                    );
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
