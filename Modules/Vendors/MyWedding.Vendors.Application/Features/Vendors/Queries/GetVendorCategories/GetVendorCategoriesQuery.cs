using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Application.Features.Vendors.Queries.GetVendorCategories
{
    public record VendorCategoryDto(Guid Id, string Name);

    public class GetVendorCategoriesQuery : IRequest<IReadOnlyList<VendorCategoryDto>>
    {
    }

    public class GetVendorCategoriesQueryHandler : IRequestHandler<GetVendorCategoriesQuery, IReadOnlyList<VendorCategoryDto>>
    {
        private readonly IVendorCategoryRepository _categoryRepository;

        public GetVendorCategoriesQueryHandler(IVendorCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IReadOnlyList<VendorCategoryDto>> Handle(
            GetVendorCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAllOrderedAsync(cancellationToken);
            return categories
                .Select(c => new VendorCategoryDto(c.Id, c.Name))
                .ToList();
        }
    }
}
