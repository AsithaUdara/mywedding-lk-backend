using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace MyWedding.Identity.Infrastructure.Authentication;

public static class FirebaseAdminSetup
{
    public static void InitializeFirebase(
        this IServiceCollection services,
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return;
        }

        var credentialsPath = configuration["Firebase:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new InvalidOperationException(
                "Firebase Credentials Path is not configured. Set 'Firebase:CredentialsPath'.");
        }

        var absolutePath = ResolveCredentialsPath(credentialsPath, contentRootPath);

        using var stream = File.OpenRead(absolutePath);
        var credential = CredentialFactory.FromStream<ServiceAccountCredential>(stream).ToGoogleCredential();
        FirebaseApp.Create(new AppOptions
        {
            Credential = credential,
        });
    }

    internal static string ResolveCredentialsPath(string credentialsPath, string? contentRootPath)
    {
        foreach (var candidate in BuildCandidatePaths(credentialsPath, contentRootPath))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Firebase credentials file not found. Checked path '{credentialsPath}' and common API project locations.");
    }

    private static IEnumerable<string> BuildCandidatePaths(string credentialsPath, string? contentRootPath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in EnumerateRawCandidates(credentialsPath, contentRootPath))
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> EnumerateRawCandidates(string credentialsPath, string? contentRootPath)
    {
        yield return Path.GetFullPath(credentialsPath);
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, credentialsPath));

        if (!string.IsNullOrWhiteSpace(contentRootPath))
        {
            yield return Path.GetFullPath(Path.Combine(contentRootPath, credentialsPath));
            yield return Path.GetFullPath(Path.Combine(contentRootPath, Path.GetFileName(credentialsPath)));
        }

        var sep = Path.DirectorySeparatorChar.ToString();
        var legacySegment = $"{sep}src{sep}Presentation{sep}";
        var currentSegment = $"{sep}Presentation{sep}";

        if (credentialsPath.Contains(legacySegment, StringComparison.OrdinalIgnoreCase))
        {
            var migrated = credentialsPath.Replace(legacySegment, currentSegment, StringComparison.OrdinalIgnoreCase);
            yield return Path.GetFullPath(migrated);
        }

        if (!Path.IsPathRooted(credentialsPath) && !string.IsNullOrWhiteSpace(contentRootPath))
        {
            var backendRoot = Directory.GetParent(contentRootPath)?.Parent?.FullName;
            if (!string.IsNullOrWhiteSpace(backendRoot))
            {
                yield return Path.GetFullPath(Path.Combine(
                    backendRoot,
                    "Presentation",
                    "MyWedding.API",
                    Path.GetFileName(credentialsPath)));
            }
        }
    }
}
