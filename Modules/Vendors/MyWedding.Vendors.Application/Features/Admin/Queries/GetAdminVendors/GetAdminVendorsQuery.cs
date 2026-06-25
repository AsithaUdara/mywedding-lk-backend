using MediatR;
using MyWedding.Domain.Enums;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors
{
    public class GetAdminVendorsQuery : IRequest<IEnumerable<AdminVendorDto>>
    {
        public VerificationStatus? Status { get; set; }
    }

    public class AdminVendorDto
    {
        public required string UserId { get; set; }
        public required string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? City { get; set; }
        public string? CategoryName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerName { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
        public int ActiveServiceCount { get; set; }
        public decimal AverageRating { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string SubscriptionTier { get; set; } = "Free";
    }
}
