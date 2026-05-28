using Microsoft.Extensions.DependencyInjection;
using MyWedding.Infrastructure.Persistence.Repositories;
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
            services.AddScoped<IVendorInquiryRepository, VendorInquiryRepository>();
            services.AddScoped<IVendorInquiryQuoteRepository, VendorInquiryQuoteRepository>();
            services.AddScoped<IVendorBlockedDateRepository, VendorBlockedDateRepository>();
            services.AddScoped<IVendorProfileViewRepository, VendorProfileViewRepository>();
            services.AddScoped<IVendorAnalyticsRepository, VendorAnalyticsRepository>();
            services.AddScoped<IBookingContractRepository, BookingContractRepository>();
            return services;
        }
    }
}
