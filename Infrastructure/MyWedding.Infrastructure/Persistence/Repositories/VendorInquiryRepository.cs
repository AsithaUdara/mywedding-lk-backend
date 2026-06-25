using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using MyWedding.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class VendorInquiryRepository : IVendorInquiryRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorInquiryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VendorInquiry inquiry, CancellationToken cancellationToken = default)
        {
            await _context.VendorInquiries.AddAsync(inquiry, cancellationToken);
        }

        public async Task<IEnumerable<VendorInquiry>> GetByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiries
                .Where(vi => vi.VendorId == vendorId)
                .OrderByDescending(vi => vi.SentAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<VendorInquiryInboxRow>> GetInboxByVendorIdAsync(
            string vendorId,
            CancellationToken cancellationToken = default)
        {
            var inquiries = await _context.VendorInquiries
                .AsNoTracking()
                .Where(vi => vi.VendorId == vendorId)
                .OrderByDescending(vi => vi.SentAt)
                .ToListAsync(cancellationToken);

            if (inquiries.Count == 0)
            {
                return Array.Empty<VendorInquiryInboxRow>();
            }

            var senderIds = inquiries.Select(i => i.SenderId).Distinct().ToList();

            var plannerIds = await _context.WeddingPlanners
                .AsNoTracking()
                .Where(p => senderIds.Contains(p.UserId))
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);
            var plannerIdSet = plannerIds.ToHashSet();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            var planners = await _context.WeddingPlanners
                .AsNoTracking()
                .Where(p => senderIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, cancellationToken);

            var explicitEventIds = inquiries
                .Where(i => i.EventId.HasValue)
                .Select(i => i.EventId!.Value)
                .Distinct()
                .ToList();

            var eventsById = await _context.WeddingEvents
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(e => explicitEventIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

            var plannerLinks = await _context.PlannerClientEvents
                .AsNoTracking()
                .Include(p => p.WeddingEvent)
                .Where(p => senderIds.Contains(p.PlannerId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            var latestEventByPlanner = plannerLinks
                .GroupBy(p => p.PlannerId)
                .ToDictionary(g => g.Key, g => g.First().WeddingEvent);

            var rows = new List<VendorInquiryInboxRow>(inquiries.Count);
            foreach (var inquiry in inquiries)
            {
                var isPlanner = plannerIdSet.Contains(inquiry.SenderId);
                users.TryGetValue(inquiry.SenderId, out var user);
                var senderName = user is null
                    ? inquiry.SenderEmail.Split('@')[0]
                    : $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(senderName))
                {
                    senderName = inquiry.SenderEmail;
                }

                string senderOrg;
                if (isPlanner && planners.TryGetValue(inquiry.SenderId, out var planner))
                {
                    senderOrg = planner.BusinessName;
                }
                else
                {
                    senderOrg = "Direct inquiry";
                }

                WeddingEvent? weddingEvent = null;
                if (inquiry.EventId.HasValue)
                {
                    eventsById.TryGetValue(inquiry.EventId.Value, out weddingEvent);
                }
                else if (isPlanner && latestEventByPlanner.TryGetValue(inquiry.SenderId, out var linked))
                {
                    weddingEvent = linked;
                }

                var budgetHint = weddingEvent is null
                    ? null
                    : $"LKR {weddingEvent.TotalBudget:N0}";

                rows.Add(new VendorInquiryInboxRow
                {
                    Id = inquiry.Id,
                    Message = inquiry.Message,
                    Subject = inquiry.Subject ?? BuildSubject(inquiry, senderName, weddingEvent?.EventName),
                    SenderEmail = inquiry.SenderEmail,
                    SenderId = inquiry.SenderId,
                    VendorId = inquiry.VendorId,
                    SentAt = inquiry.SentAt,
                    IsRead = inquiry.IsRead,
                    From = isPlanner ? "planner" : "client",
                    SenderName = senderName,
                    SenderOrg = senderOrg,
                    EventName = weddingEvent?.EventName,
                    WeddingDate = weddingEvent?.EventDate,
                    BudgetHint = budgetHint
                });
            }

            return rows;
        }

        public async Task<VendorInquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiries.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<VendorInquiry?> GetByIdForVendorAsync(
            Guid id,
            string vendorId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VendorInquiries
                .FirstOrDefaultAsync(i => i.Id == id && i.VendorId == vendorId, cancellationToken);
        }

        private static string BuildSubject(VendorInquiry inquiry, string senderName, string? eventName)
        {
            if (!string.IsNullOrWhiteSpace(inquiry.Subject))
            {
                return inquiry.Subject;
            }

            if (!string.IsNullOrWhiteSpace(eventName))
            {
                return $"Inquiry — {eventName}";
            }

            return $"Inquiry from {senderName}";
        }
    }
}
