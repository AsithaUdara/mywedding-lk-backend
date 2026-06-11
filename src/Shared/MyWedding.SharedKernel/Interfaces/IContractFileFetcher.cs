namespace MyWedding.SharedKernel.Interfaces;

public interface IContractFileFetcher
{
    Task<byte[]> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default);
}
