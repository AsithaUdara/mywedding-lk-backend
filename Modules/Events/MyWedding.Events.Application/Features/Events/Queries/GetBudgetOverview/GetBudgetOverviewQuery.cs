// File: src/Core/MyWedding.Application/Features/Events/Queries/GetBudgetOverview/GetBudgetOverviewQuery.cs
using MediatR;
using System;

namespace MyWedding.Events.Application.Features.Events.Queries.GetBudgetOverview
{
    public class GetBudgetOverviewQuery : IRequest<BudgetOverviewDto?>
    {
        public Guid EventId { get; init; }
    }
}
