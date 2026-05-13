// File: src/Core/MyWedding.Application/Features/Vendors/Queries/GetVendorServices/GetVendorServicesQuery.cs
using MediatR;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorServices
{
    public class GetVendorServicesQuery : IRequest<IEnumerable<VendorServiceDto>>
    {
        public required string VendorId { get; set; }
    }
}
