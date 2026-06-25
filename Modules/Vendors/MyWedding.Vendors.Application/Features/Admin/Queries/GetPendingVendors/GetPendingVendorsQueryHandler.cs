using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPendingVendors
{
    public class GetPendingVendorsQueryHandler : IRequestHandler<GetPendingVendorsQuery, IEnumerable<PendingVendorDto>>
    {
        private readonly IVendorRepository _vendorRepository;

        public GetPendingVendorsQueryHandler(IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<IEnumerable<PendingVendorDto>> Handle(GetPendingVendorsQuery request, CancellationToken cancellationToken)
        {
            var vendors = await _vendorRepository.GetPendingVendorsAsync(cancellationToken);

            return vendors.Select(v => new PendingVendorDto
            {
                UserId            = v.UserId,
                BusinessName      = v.BusinessName,
                BusinessDescription = v.BusinessDescription,
                City              = v.City,
                CategoryName      = v.PrimaryCategory?.Name,
                OwnerEmail        = v.User?.Email,
                OwnerName         = v.User != null ? $"{v.User.FirstName} {v.User.LastName}" : null,
                VerificationStatus = v.VerificationStatus.ToString()
            });
        }
    }
}
