using MediatR;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorById
{
    public class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, VendorDetailDto>
    {
        private readonly IVendorRepository _vendorRepository;

        public GetVendorByIdQueryHandler(IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<VendorDetailDto> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);

            if (vendor is null)
            {
                throw new NotFoundException(nameof(Vendor), request.VendorId);
            }

            // Map to DTOs, including nested data like Services and Reviews
            var vendorDetailDto = new VendorDetailDto
            {
                UserId = vendor.UserId,
                BusinessName = vendor.BusinessName,
                BusinessDescription = vendor.BusinessDescription,
                WebsiteUrl = vendor.WebsiteUrl,
                City = vendor.City ?? "N/A",
                VerificationStatus = vendor.VerificationStatus,
                AverageRating = vendor.AverageRating,
                Services = vendor.Services.Select(s => new VendorServiceDto(
                    s.Id, 
                    s.ServiceName, 
                    s.ServiceDescription ?? "", 
                    s.BasePrice, 
                    s.PricingType
                )),
                Reviews = vendor.Reviews.Select(r => new VendorReviewDto(
                    r.Id, 
                    (r.Reviewer?.FirstName ?? "") + " " + (r.Reviewer?.LastName ?? ""), 
                    r.Rating, 
                    r.ReviewContent ?? "", 
                    r.CreatedAt
                ))
            };

            return vendorDetailDto;
        }
    }
}
