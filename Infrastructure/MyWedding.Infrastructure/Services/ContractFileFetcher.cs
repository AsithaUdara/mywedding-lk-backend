using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class ContractFileFetcher : IContractFileFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ContractFileFetcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(nameof(ContractFileFetcher));
        var candidates = BuildCandidateUrls(fileUrl);

        foreach (var candidate in candidates)
        {
            using var response = await client.GetAsync(candidate, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (IsValidPdf(bytes))
                return bytes;
        }

        throw new HttpRequestException($"Could not download a valid PDF from {fileUrl}");
    }

    private static IEnumerable<string> BuildCandidateUrls(string fileUrl)
    {
        yield return fileUrl;

        if (fileUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            yield return fileUrl[..^4];
        }
        else if (fileUrl.Contains("/image/upload/", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{fileUrl}.pdf";
        }
    }

    private static bool IsValidPdf(byte[] bytes) =>
        bytes.Length >= 5
        && bytes[0] == (byte)'%'
        && bytes[1] == (byte)'P'
        && bytes[2] == (byte)'D'
        && bytes[3] == (byte)'F';
}
