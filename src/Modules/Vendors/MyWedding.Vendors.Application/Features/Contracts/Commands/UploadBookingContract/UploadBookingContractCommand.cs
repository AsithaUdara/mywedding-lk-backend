using MediatR;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.UploadBookingContract;

public class UploadBookingContractCommand : IRequest<UploadBookingContractResult>
{
    public required Guid BookingId { get; init; }
    public required string VendorUserId { get; init; }
    public byte[]? PdfBytes { get; init; }
    public bool GenerateStandardContract { get; init; }
}

public class UploadBookingContractResult
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public required string ContractFileUrl { get; init; }
    public DateTime UploadedAtUtc { get; init; }
    public bool SentToClient { get; init; }
}
