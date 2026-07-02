using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.ReadModels;
using System.Collections.Generic;

namespace MyWedding.Vendors.Application.Features.Admin.Queries.GetAdminVendors;

public class GetAdminVendorsQuery : IRequest<PagedResult<AdminVendorDto>>
{
    public VerificationStatus? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
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
