using Microsoft.Extensions.DependencyInjection;
using MyWedding.Infrastructure.Services;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.DependencyInjection;

public static class ServiceStubExtensions
{
    public static IServiceCollection AddServiceStubs(this IServiceCollection services)
    {
        services.AddHttpClient<IAiCopilotService, OpenAiCopilotService>();
        services.AddScoped<IPaymentGatewayService, PayHerePaymentGatewayService>();
        return services;
    }
}
