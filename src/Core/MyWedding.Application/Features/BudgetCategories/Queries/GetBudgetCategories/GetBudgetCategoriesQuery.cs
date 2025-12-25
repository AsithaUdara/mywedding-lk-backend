// File: src/Core/MyWedding.Application/Features/BudgetCategories/Queries/GetBudgetCategories/GetBudgetCategoriesQuery.cs
using MediatR;
using System.Collections.Generic;

namespace MyWedding.Application.Features.BudgetCategories.Queries.GetBudgetCategories
{
    public class GetBudgetCategoriesQuery : IRequest<IEnumerable<BudgetCategoryDto>>
    {
    }
}
