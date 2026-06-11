using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Helpers;
using MyWedding.Vendors.Application.Helpers;

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
        private readonly IVendorProfileViewRepository _profileViewRepository;

        public GetVendorByIdQueryHandler(
            IVendorRepository vendorRepository,
            IVendorProfileViewRepository profileViewRepository)
        {
            _vendorRepository = vendorRepository;
            _profileViewRepository = profileViewRepository;
        }

        public async Task<VendorDetailDto> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                await _profileViewRepository.RecordViewAsync(request.VendorId, cancellationToken);
            }
            catch
            {
                // Analytics must not block public vendor profile loads.
            }

            var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);

            if (vendor is null)
            {
                throw new NotFoundException(nameof(Vendor), request.VendorId);
            }

            if (vendor.VerificationStatus != VerificationStatus.Verified)
            {
                throw new NotFoundException(nameof(Vendor), request.VendorId);
            }

            var activeServices = VendorMediaHelper.ActiveServices(vendor).ToList();
            if (activeServices.Count == 0)
            {
                throw new NotFoundException(nameof(Vendor), request.VendorId);
            }

            var vendorDetailDto = new VendorDetailDto
            {
                UserId = vendor.UserId,
                BusinessName = vendor.BusinessName,
                BusinessDescription = vendor.BusinessDescription,
                WebsiteUrl = vendor.WebsiteUrl,
                ContactPhone = vendor.ContactPhone,
                City = vendor.City ?? "N/A",
                Province = vendor.Province,
                VerificationStatus = vendor.VerificationStatus,
                AverageRating = vendor.AverageRating,
                CoverImageUrl = vendor.CoverImageUrl,
                GalleryImageUrls = VendorMediaHelper.ResolveGalleryUrls(vendor, activeServices),
                Services = activeServices.Select(s => new VendorServiceDto(
                    s.Id,
                    s.ServiceName,
                    s.ServiceDescription ?? "",
                    s.BasePrice,
                    s.PricingType,
                    s.PrimaryImageUrl,
                    GalleryUrlHelper.Parse(s.GalleryUrlsJson),
                    s.Tagline,
                    s.ListingDetailsJson
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
