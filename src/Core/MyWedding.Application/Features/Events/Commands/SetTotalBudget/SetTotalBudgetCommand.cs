// File: src/Core/MyWedding.Application/Features/Events/Commands/SetTotalBudget/SetTotalBudgetCommand.cs
using MediatR;
using System;

namespace MyWedding.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommand : IRequest
    {
        public Guid EventId { get; init; }
        public decimal TotalBudget { get; init; }
    }
}
