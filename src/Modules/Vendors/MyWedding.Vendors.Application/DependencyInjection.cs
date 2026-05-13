using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MyWedding.Vendors.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddVendorsApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}
