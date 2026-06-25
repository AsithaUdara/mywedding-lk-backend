// File: src/Core/MyWedding.Application/Features/BudgetCategories/Queries/GetBudgetCategories/GetBudgetCategoriesQueryHandler.cs
using MediatR;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Budget.Application.Features.BudgetCategories.Queries.GetBudgetCategories
{
    public class GetBudgetCategoriesQueryHandler : IRequestHandler<GetBudgetCategoriesQuery, IEnumerable<BudgetCategoryDto>>
    {
        private readonly IBudgetCategoryRepository _categoryRepository;

        public GetBudgetCategoriesQueryHandler(IBudgetCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<BudgetCategoryDto>> Handle(GetBudgetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            return categories.Select(c => new BudgetCategoryDto(c.Id, c.Name));
        }
    }
}
