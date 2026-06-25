using MediatR;
using System;

namespace MyWedding.Events.Application.Features.Events.Commands.SetTotalBudget
{
    public class SetTotalBudgetCommand : IRequest
    {
        public Guid EventId { get; init; }
        public decimal TotalBudget { get; init; }
        public string? UserId { get; set; }
    }
}
