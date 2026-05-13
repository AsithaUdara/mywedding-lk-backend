using MediatR;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors
{
    public class GetVendorsQueryHandler : IRequestHandler<GetVendorsQuery, IEnumerable<VendorDto>>
    {
        private readonly IVendorRepository _vendorRepository;

        public GetVendorsQueryHandler(IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<IEnumerable<VendorDto>> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
        {
            var vendors = await _vendorRepository.GetAllAsync(cancellationToken);

            // Apply filtering based on request parameters (if they are provided)
            if (!string.IsNullOrEmpty(request.Category))
            {
                vendors = vendors.Where(v => v.Services.Any(s => s.Category!.Name.Contains(request.Category, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(request.Location))
            {
                vendors = vendors.Where(v => (v.City ?? "").Contains(request.Location, StringComparison.OrdinalIgnoreCase));
            }

            // Map to DTOs
            return vendors.Select(v => new VendorDto(
                v.UserId,
                v.BusinessName,
                v.BusinessDescription,
                v.WebsiteUrl,
                v.City ?? "N/A", // Provide defaults if null
                v.VerificationStatus.ToString(),
                v.AverageRating,
                v.Services.FirstOrDefault()?.Category?.Name ?? "Uncategorized" // Get the category name
            ));
        }
    }
}
