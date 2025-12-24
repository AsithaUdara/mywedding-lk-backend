using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;

namespace MyWedding.Infrastructure.Authentication;

public static class FirebaseAdminSetup
{
    public static void InitializeFirebase(this IServiceCollection services)
    {
        var credential = GoogleCredential.FromFile("firebase-credentials.json");
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
        });
    }
}