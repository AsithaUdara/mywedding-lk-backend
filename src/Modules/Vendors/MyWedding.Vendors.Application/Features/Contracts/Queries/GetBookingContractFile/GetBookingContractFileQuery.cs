using MediatR;

namespace MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContractFile;

public class GetBookingContractFileQuery : IRequest<BookingContractFileDto?>
{
    public required Guid BookingId { get; init; }
    public required string UserId { get; init; }
}

public record BookingContractFileDto(
    byte[] PdfBytes,
    string FileName,
    string ContentType);
