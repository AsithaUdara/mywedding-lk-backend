using Microsoft.Extensions.DependencyInjection;
using MyWedding.Infrastructure.Services.Mocks;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.DependencyInjection;

public static class ServiceStubExtensions
{
    public static IServiceCollection AddServiceStubs(this IServiceCollection services)
    {
        services.AddScoped<IAiCopilotService, MockAiCopilotService>();
        services.AddScoped<IPaymentGatewayService, MockPaymentGatewayService>();
        return services;
    }
}
