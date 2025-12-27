using MediatR;

namespace MyWedding.Application.Features.Vendors.Queries.GetVendorById
{
    public class GetVendorByIdQuery : IRequest<VendorDetailDto?>
    {
        public required string VendorId { get; init; } // Firebase UID of the vendor user
    }
}
