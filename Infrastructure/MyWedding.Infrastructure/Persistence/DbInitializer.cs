using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;

namespace MyWedding.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            bool applyMigrations = true,
            bool seedData = true)
        {
            if (applyMigrations)
            {
                await context.Database.MigrateAsync();
            }

            if (!seedData)
            {
                return;
            }

            await SeedReferenceDataAsync(context);
        }

        private static async Task SeedReferenceDataAsync(ApplicationDbContext context)
        {
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

            if (!context.BudgetCategories.Any())
            {
                var budgetCategories = new[]
                {
                    new BudgetCategory { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Venue", Description = "Hotels, Estates, Beachfronts" },
                    new BudgetCategory { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Photography", Description = "Capture precious moments" },
                    new BudgetCategory { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "Catering", Description = "Exquisite food & service" },
                    new BudgetCategory { Id = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), Name = "Floral & Decor", Description = "Stunning arrangements" },
                    new BudgetCategory { Id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"), Name = "Music & DJ", Description = "Unforgettable entertainment" },
                    new BudgetCategory { Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"), Name = "Other", Description = "Planning, Cakes, Rentals etc" }
                };

                await context.BudgetCategories.AddRangeAsync(budgetCategories);
                await context.SaveChangesAsync();
            }

            var eventsWithoutConversations = await context.WeddingEvents
                .Where(e => !context.Conversations.Any(c => c.EventId == e.Id))
                .ToListAsync();

            if (eventsWithoutConversations.Count > 0)
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

            await NormalizeLegacyVendorDepositExpenseTitlesAsync(context);
        }

        private static async Task NormalizeLegacyVendorDepositExpenseTitlesAsync(ApplicationDbContext context)
        {
            const string legacyPrefix = "Vendor Deposit - ";
            var legacyExpenses = await context.Expenses
                .Where(e => e.Title.StartsWith(legacyPrefix))
                .ToListAsync();

            if (legacyExpenses.Count == 0)
                return;

            var parsedLegacy = new List<(Expense Expense, Guid BookingId)>();
            foreach (var expense in legacyExpenses)
            {
                var bookingToken = expense.Title[legacyPrefix.Length..].Trim();
                if (Guid.TryParse(bookingToken, out var bookingId))
                {
                    parsedLegacy.Add((expense, bookingId));
                }
            }

            if (parsedLegacy.Count == 0)
                return;

            var bookingIds = parsedLegacy.Select(x => x.BookingId).Distinct().ToList();
            var bookings = await context.VendorBookings
                .Include(b => b.VendorService)
                    .ThenInclude(s => s!.Vendor)
                .Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id);

            var updated = false;
            foreach (var (expense, bookingId) in parsedLegacy)
            {
                if (!bookings.TryGetValue(bookingId, out var booking))
                    continue;

                var serviceName = booking.VendorService?.ServiceName?.Trim();
                var vendorName = booking.VendorService?.Vendor?.BusinessName?.Trim();
                var label = string.Join(" - ", new[] { vendorName, serviceName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var shortBookingId = booking.Id.ToString("N")[..8];
                var normalizedTitle = string.IsNullOrWhiteSpace(label)
                    ? $"Vendor deposit ({shortBookingId})"
                    : $"Vendor deposit - {label} ({shortBookingId})";

                if (!string.Equals(expense.Title, normalizedTitle, StringComparison.Ordinal))
                {
                    expense.Title = normalizedTitle;
                    expense.UpdatedAt = DateTime.UtcNow;
                    updated = true;
                }
            }

            if (updated)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
