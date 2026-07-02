using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyWedding.Infrastructure.Services;
using MyWedding.Infrastructure.Services.Mocks;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.DependencyInjection;

public static class IntegrationServiceExtensions
{
    /// <summary>
    /// Registers external integrations. Gmail SMTP is required unless Integrations:AllowMockEmail=true.
    /// </summary>
    public static IServiceCollection AddExternalIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterEmailService(services, configuration);
        RegisterMediaServices(services);
        return services;
    }

    public static void LogIntegrationStatus(IConfiguration configuration)
    {
        var smtp = IsSmtpConfigured(configuration);
        var allowMockEmail = configuration.GetValue("Integrations:AllowMockEmail", false);
        var openAi = !string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]);
        var payHere = !string.IsNullOrWhiteSpace(configuration["PayHere:MerchantSecret"]);
        var cloudinary = IsCloudinaryConfigured(configuration);

        Console.WriteLine();
        Console.WriteLine("=== MyWedding.lk integration status ===");
        Console.WriteLine($"  Email (SMTP):      {(smtp ? "LIVE" : allowMockEmail ? "MOCK (dev)" : "MISSING — set Smtp:Password")}");
        Console.WriteLine($"  OpenAI:            {(openAi ? "LIVE" : "simulated fallback")}");
        Console.WriteLine($"  PayHere:           {(payHere ? "LIVE (signed checkout)" : "sandbox, no hash")}");
        Console.WriteLine($"  Cloudinary:        {(cloudinary ? "LIVE (PDF + assets)" : "MISSING — set Cloudinary:* secrets")}");
        Console.WriteLine("  See docs/REAL_API_SETUP.md");
        Console.WriteLine();
    }

    private static bool IsCloudinaryConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Cloudinary:CloudName"])
        && !string.IsNullOrWhiteSpace(configuration["Cloudinary:ApiKey"])
        && !string.IsNullOrWhiteSpace(configuration["Cloudinary:ApiSecret"]);

    private static void RegisterMediaServices(IServiceCollection services)
    {
        services.AddHttpClient(nameof(ContractFileFetcher));
        services.AddScoped<IContractFileFetcher, ContractFileFetcher>();
        services.AddSingleton<IQuotePdfGenerator, InquiryQuotePdfGenerator>();
        services.AddSingleton<ICloudinaryMediaStorage, CloudinaryMediaStorage>();
        services.AddScoped<IWeddingPlannerProfileReader, WeddingPlannerProfileReader>();
    }

    private static bool IsSmtpConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Smtp:Password"])
        && !string.IsNullOrWhiteSpace(configuration["Smtp:Username"]);

    private static void RegisterEmailService(IServiceCollection services, IConfiguration configuration)
    {
        var allowMockEmail = configuration.GetValue("Integrations:AllowMockEmail", false);

        if (IsSmtpConfigured(configuration))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
            return;
        }

        if (allowMockEmail)
        {
            services.AddScoped<IEmailService, MockEmailService>();
            return;
        }

        throw new InvalidOperationException(
            "SMTP is not configured. Set Smtp:Username and Smtp:Password via user secrets. " +
            "See docs/REAL_API_SETUP.md. To use console-only mock emails locally, set Integrations:AllowMockEmail=true.");
    }
}
