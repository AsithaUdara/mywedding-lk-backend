using MediatR;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetPendingVendors
{
    public class GetPendingVendorsQuery : IRequest<IEnumerable<PendingVendorDto>> { }

    public class PendingVendorDto
    {
        public required string UserId { get; set; }
        public required string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? City { get; set; }
        public string? CategoryName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerName { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
    }
}
