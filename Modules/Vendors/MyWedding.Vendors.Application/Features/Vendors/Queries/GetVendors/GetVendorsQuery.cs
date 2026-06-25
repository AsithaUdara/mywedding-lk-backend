using MediatR;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendors
{
    public class GetVendorsQuery : IRequest<IEnumerable<VendorDto>>
    {
        // We can add filtering parameters here later, e.g., Category, Location
        public string? Category { get; set; }
        public string? Location { get; set; }
    }
}
