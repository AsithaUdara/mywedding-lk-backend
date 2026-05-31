using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class CloudinaryMediaStorage : ICloudinaryMediaStorage
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryMediaStorage(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary is not configured. Set Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret " +
                "via user secrets. See docs/REAL_API_SETUP.md.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadPdfAsync(
        byte[] pdfBytes,
        string folder,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(pdfBytes);

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription($"{publicId}.pdf", stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
        };

        var result = await Task.Run(() => _cloudinary.Upload(uploadParams), cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(
                $"Cloudinary PDF upload failed: {result.Error.Message}");
        }

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Cloudinary did not return a secure URL for the uploaded PDF.");
        }

        return url;
    }
}
