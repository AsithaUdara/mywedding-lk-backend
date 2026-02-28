// File: src/Core/MyWedding.Application/Features/Vendors/Queries/GetVendorServices/VendorServiceDto.cs
using MyWedding.Domain.Enums;
using System;

namespace MyWedding.Application.Features.Vendors.Queries.GetVendorServices
{
    public class VendorServiceDto
    {
        public Guid Id { get; set; }
        public required string ServiceName { get; set; }
        public string? ServiceDescription { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public required string CategoryName { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; }
    }
}
