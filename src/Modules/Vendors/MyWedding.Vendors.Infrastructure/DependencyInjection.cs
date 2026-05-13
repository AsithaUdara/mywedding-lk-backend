using Microsoft.Extensions.DependencyInjection;
using MyWedding.Vendors.Infrastructure.Persistence.Repositories;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddVendorsModule(this IServiceCollection services)
        {
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IVendorServiceRepository, VendorServiceRepository>();
            services.AddScoped<IVendorBookingRepository, VendorBookingRepository>();
            return services;
        }
    }
}
