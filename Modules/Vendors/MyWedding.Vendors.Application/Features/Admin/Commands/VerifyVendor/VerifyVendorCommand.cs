using MediatR;

namespace MyWedding.Vendors.Application.Features.Admin.Commands.VerifyVendor
{
    public class VerifyVendorCommand : IRequest<bool>
    {
        public required string VendorId { get; set; }
        /// <summary>True = Verified, False = Rejected.</summary>
        public bool IsApproved { get; set; }
    }
}
