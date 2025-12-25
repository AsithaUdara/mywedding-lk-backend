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
        // Hot reload / multiple initialization protection
        if (FirebaseApp.DefaultInstance != null)
        {
            return;
        }

        var credentialsPath = configuration["Firebase:CredentialsPath"];

        if (string.IsNullOrEmpty(credentialsPath))
        {
            throw new InvalidOperationException("Firebase Credentials Path is not configured. Set 'Firebase:CredentialsPath'.");
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, credentialsPath);

        if (!File.Exists(absolutePath))
        {
            absolutePath = Path.GetFullPath(credentialsPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Firebase credentials file not found at path: {absolutePath}");
            }
        }

        var credential = GoogleCredential.FromFile(absolutePath);
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
        });
    }
}