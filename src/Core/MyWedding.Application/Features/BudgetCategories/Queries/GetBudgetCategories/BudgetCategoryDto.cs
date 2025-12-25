// File: src/Core/MyWedding.Application/Features/BudgetCategories/Queries/GetBudgetCategories/BudgetCategoryDto.cs
using System;

namespace MyWedding.Application.Features.BudgetCategories.Queries.GetBudgetCategories
{
    public record BudgetCategoryDto(Guid Id, string Name);
}
