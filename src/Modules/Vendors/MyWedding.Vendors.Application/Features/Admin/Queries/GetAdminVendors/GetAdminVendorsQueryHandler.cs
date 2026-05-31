using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors
{
    public class GetAdminVendorsQueryHandler : IRequestHandler<GetAdminVendorsQuery, IEnumerable<AdminVendorDto>>
    {
        private readonly IVendorRepository _vendorRepository;

        public GetAdminVendorsQueryHandler(IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<IEnumerable<AdminVendorDto>> Handle(
            GetAdminVendorsQuery request,
            CancellationToken cancellationToken)
        {
            var vendors = await _vendorRepository.GetAdminVendorsAsync(request.Status, cancellationToken);

            return vendors.Select(v => new AdminVendorDto
            {
                UserId = v.UserId,
                BusinessName = v.BusinessName,
                BusinessDescription = v.BusinessDescription,
                City = v.City,
                CategoryName = v.PrimaryCategory?.Name,
                OwnerEmail = v.User?.Email,
                OwnerName = v.User != null ? $"{v.User.FirstName} {v.User.LastName}" : null,
                VerificationStatus = v.VerificationStatus.ToString(),
                ActiveServiceCount = v.Services.Count(s => s.IsActive),
                AverageRating = v.AverageRating,
                RegisteredAt = v.User?.CreatedAt ?? DateTime.UtcNow,
            }).ToList();
        }
    }
}
