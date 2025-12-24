using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace MyWedding.Infrastructure.Authentication;

public static class FirebaseAdminSetup
{
    public static void InitializeFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        // Prevent re-initialization of FirebaseApp
        try
        {
            var _ = FirebaseApp.DefaultInstance;
            // If we reach here, a default instance already exists
            return;
        }
        catch (InvalidOperationException)
        {
            // No default instance, continue with initialization
        }

        var credentialsPath = configuration["Firebase:CredentialsPath"];

        if (string.IsNullOrEmpty(credentialsPath))
        {
            throw new InvalidOperationException("Firebase Credentials Path is not configured. Set 'Firebase:CredentialsPath' in user secrets or configuration.");
        }

        // Ensure the path is correct relative to the application's base directory
        var absolutePath = Path.Combine(AppContext.BaseDirectory, credentialsPath);

        if (!File.Exists(absolutePath))
        {
            // Fallback for running from a different directory (like the repo root)
            absolutePath = Path.GetFullPath(credentialsPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Firebase credentials file not found at path: {credentialsPath}");
            }
        }

        var credential = GoogleCredential.FromFile(absolutePath);
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
        });
    }
}