using Microsoft.Extensions.DependencyInjection;
using MyWedding.Budget.Infrastructure.Persistence.Repositories;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Budget.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBudgetModule(this IServiceCollection services)
        {
            services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            return services;
        }
    }
}
