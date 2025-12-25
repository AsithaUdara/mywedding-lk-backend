// File: src/Core/MyWedding.Application/Features/Events/Queries/GetBudgetOverview/BudgetOverviewDto.cs
using System;

namespace MyWedding.Application.Features.Events.Queries.GetBudgetOverview
{
    public record BudgetOverviewDto(
        Guid EventId,
        decimal TotalBudget,
        decimal TotalSpent,
        decimal RemainingBudget
    );
}
