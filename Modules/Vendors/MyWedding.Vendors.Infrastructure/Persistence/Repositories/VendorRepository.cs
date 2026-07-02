using Microsoft.EntityFrameworkCore;


using MyWedding.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Infrastructure.Persistence.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Vendor?> GetByIdAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            // Use .Include() to eagerly load all related data needed for the detail page
            return await _context.Vendors
                .Include(v => v.User) // Join with Users table to get email
                .Include(v => v.Services)
                    .ThenInclude(s => s.Category) // Also join the Category for each Service
                .Include(v => v.Reviews)
                    .ThenInclude(r => r.Reviewer) // Join with Users table again to get the reviewer's name
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == vendorId, cancellationToken);
        }

        public async Task<IEnumerable<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Implementation...
            IQueryable<Vendor> query = _context.Vendors
                .Include(v => v.User)
                .Include(v => v.PrimaryCategory)
                .Include(v => v.Services)
                    .ThenInclude(s => s.Category)
                .Include(v => v.Reviews)
                .AsNoTracking();

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Vendor>> GetPendingVendorsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Vendors
                .Where(v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Pending)
                .Include(v => v.User)
                .Include(v => v.PrimaryCategory)
                .AsNoTracking()
                .OrderBy(v => v.BusinessName)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Vendor>> GetAdminVendorsPagedAsync(
            MyWedding.Domain.Enums.VerificationStatus? status,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Vendor> query = _context.Vendors
                .Include(v => v.User)
                .Include(v => v.PrimaryCategory)
                .Include(v => v.Services)
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(v => v.VerificationStatus == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(v =>
                    v.BusinessName.ToLower().Contains(term) ||
                    v.UserId.ToLower().Contains(term) ||
                    (v.City != null && v.City.ToLower().Contains(term)) ||
                    (v.BusinessDescription != null && v.BusinessDescription.ToLower().Contains(term)) ||
                    (v.User != null && v.User.Email != null && v.User.Email.ToLower().Contains(term)) ||
                    (v.User != null && (
                        v.User.FirstName.ToLower().Contains(term) ||
                        v.User.LastName.ToLower().Contains(term))) ||
                    (v.PrimaryCategory != null && v.PrimaryCategory.Name.ToLower().Contains(term)));
            }

            query = query
                .OrderByDescending(v => v.User != null ? v.User.CreatedAt : DateTime.MinValue)
                .ThenBy(v => v.BusinessName);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Vendor>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<AdminVendorSummary> GetAdminVendorSummaryAsync(CancellationToken cancellationToken = default)
        {
            var vendors = _context.Vendors.AsNoTracking();

            var total = await vendors.CountAsync(cancellationToken);
            var verified = await vendors.CountAsync(
                v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Verified,
                cancellationToken);
            var pending = await vendors.CountAsync(
                v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Pending,
                cancellationToken);
            var rejected = await vendors.CountAsync(
                v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Rejected,
                cancellationToken);
            var liveListings = await vendors.CountAsync(
                v => v.VerificationStatus == MyWedding.Domain.Enums.VerificationStatus.Verified &&
                     v.Services.Any(s => s.IsActive),
                cancellationToken);
            var categories = await vendors
                .Where(v => v.PrimaryCategoryId != null)
                .Select(v => v.PrimaryCategoryId)
                .Distinct()
                .CountAsync(cancellationToken);

            return new AdminVendorSummary
            {
                Total = total,
                Verified = verified,
                Pending = pending,
                Rejected = rejected,
                LiveListings = liveListings,
                Categories = categories,
            };
        }

        public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
        {
            await _context.Vendors.AddAsync(vendor, cancellationToken);
        }

        public void Update(Vendor vendor)
        {
            _context.Vendors.Update(vendor);
        }

        public async Task<bool> SetVerificationStatusAsync(
            string vendorId,
            MyWedding.Domain.Enums.VerificationStatus status,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.Vendors
                .Where(v => v.UserId == vendorId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(v => v.VerificationStatus, status),
                    cancellationToken);
            return rows > 0;
        }
    }
}
