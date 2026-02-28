// File: src/Core/MyWedding.Application/Features/Vendors/Commands/AddService/AddServiceCommand.cs
using MediatR;
using MyWedding.Domain.Enums;
using System;

namespace MyWedding.Application.Features.Vendors.Commands.AddService
{
    public class AddServiceCommand : IRequest<Guid>
    {
        public required string VendorId { get; set; }
        public required string ServiceName { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public Guid CategoryId { get; set; }
    }
}
