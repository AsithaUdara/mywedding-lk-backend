using Microsoft.Extensions.DependencyInjection;
using MyWedding.Collaboration.Infrastructure.Persistence.Repositories;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Collaboration.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCollaborationModule(this IServiceCollection services)
        {
            services.AddScoped<IActivityFeedRepository, ActivityFeedRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IPollRepository, PollRepository>();
            return services;
        }
    }
}
