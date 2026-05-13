// File: src/Core/MyWedding.Application/Features/Vendors/Queries/GetVendorServices/GetVendorServicesQueryHandler.cs
using MediatR;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorServices
{
    public class GetVendorServicesQueryHandler : IRequestHandler<GetVendorServicesQuery, IEnumerable<VendorServiceDto>>
    {
        private readonly IVendorServiceRepository _serviceRepository;

        public GetVendorServicesQueryHandler(IVendorServiceRepository serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<IEnumerable<VendorServiceDto>> Handle(GetVendorServicesQuery request, CancellationToken cancellationToken)
        {
            var services = await _serviceRepository.GetByVendorIdAsync(request.VendorId, cancellationToken);

            return services.Select(s => new VendorServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                ServiceDescription = s.ServiceDescription,
                BasePrice = s.BasePrice,
                PricingType = s.PricingType,
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name ?? "Unknown",
                IsActive = s.IsActive
            });
        }
    }
}
