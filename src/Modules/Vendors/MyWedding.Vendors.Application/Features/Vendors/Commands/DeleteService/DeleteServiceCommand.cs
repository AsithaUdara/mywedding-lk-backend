// File: src/Core/MyWedding.Application/Features/Vendors/Commands/DeleteService/DeleteServiceCommand.cs
using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.DeleteService
{
    public class DeleteServiceCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
