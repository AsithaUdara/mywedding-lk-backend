namespace MyWedding.SharedKernel.Interfaces;

public interface ICloudinaryMediaStorage
{
  /// <summary>Uploads a PDF and returns the Cloudinary secure URL.</summary>
  Task<string> UploadPdfAsync(
    byte[] pdfBytes,
    string folder,
    string publicId,
    CancellationToken cancellationToken = default);
}
