// File: src/Core/MyWedding.Application/Features/Vendors/Commands/RegisterVendor/RegisterVendorCommand.cs
using MediatR;
using System;

namespace MyWedding.Application.Features.Vendors.Commands.RegisterVendor
{
    public class RegisterVendorCommand : IRequest<Guid>
    {
        public required string UserId { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string BusinessName { get; set; }
        public required string Category { get; set; } // The ID as string from frontend
        public required string City { get; set; }
    }
}
