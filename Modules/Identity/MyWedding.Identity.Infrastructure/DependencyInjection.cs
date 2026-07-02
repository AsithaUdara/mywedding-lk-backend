using Microsoft.Extensions.DependencyInjection;
using MyWedding.Identity.Infrastructure.Persistence.Repositories;
using MyWedding.Identity.Infrastructure.Authentication;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityModule(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
            return services;
        }
    }
}
