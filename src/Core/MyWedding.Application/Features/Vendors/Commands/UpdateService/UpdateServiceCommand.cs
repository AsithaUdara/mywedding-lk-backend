// File: src/Core/MyWedding.Application/Features/Vendors/Commands/UpdateService/UpdateServiceCommand.cs
using MediatR;
using MyWedding.Domain.Enums;
using System;

namespace MyWedding.Application.Features.Vendors.Commands.UpdateService
{
    public class UpdateServiceCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public required string ServiceName { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public PricingType PricingType { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; }
    }
}
