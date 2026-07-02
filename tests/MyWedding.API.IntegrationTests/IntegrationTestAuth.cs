using System.Net.Http.Headers;
using MyWedding.API.Auth.Testing;

namespace MyWedding.API.IntegrationTests;

public static class IntegrationTestAuth
{
    public static void AuthenticateAs(this HttpClient client, string userId, string role = "user")
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.SchemeName, $"{userId}|{role}");
    }

    public static void ClearAuthentication(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }
}
