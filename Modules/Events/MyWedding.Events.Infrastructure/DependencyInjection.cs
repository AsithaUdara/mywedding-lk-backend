using Microsoft.Extensions.DependencyInjection;
using MyWedding.Events.Infrastructure.Persistence.Repositories;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Events.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEventsModule(this IServiceCollection services)
        {
            services.AddScoped<IWeddingEventRepository, WeddingEventRepository>();
            services.AddScoped<IEventOrganizerRepository, EventOrganizerRepository>();
            services.AddScoped<MyWedding.SharedKernel.Security.IEventAuthorizationService, MyWedding.Events.Infrastructure.Security.EventAuthorizationService>();
            services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();
            return services;
        }
    }
}
