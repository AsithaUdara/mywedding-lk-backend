using MediatR;

namespace MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContract;

public class GetBookingContractQuery : IRequest<BookingContractDto?>
{
    public required Guid BookingId { get; init; }
    public required string UserId { get; init; }
}

public record BookingContractDto(
    Guid BookingId,
    Guid EventId,
    string? ContractFileUrl,
    DateTime? ContractUploadedAt,
    DateTime? SentToClientAt,
    DateTime? ClientSignedAt,
    string BookingStatus,
    bool RequiresContractBeforePayment,
    bool CanPreview,
    bool CanSendToClient,
    bool CanSign,
    bool CanPayDeposit);
