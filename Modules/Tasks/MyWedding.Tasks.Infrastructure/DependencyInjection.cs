using Microsoft.Extensions.DependencyInjection;
using MyWedding.Tasks.Infrastructure.Persistence.Repositories;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Tasks.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddTasksModule(this IServiceCollection services)
        {
            services.AddScoped<IEventTaskRepository, EventTaskRepository>();
            return services;
        }
    }
}
