using MediatR;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SendBookingContract;

public class SendBookingContractCommand : IRequest<SendBookingContractResult>
{
    public required Guid BookingId { get; init; }
    public required string VendorUserId { get; init; }
}

public class SendBookingContractResult
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public DateTime SentToClientAtUtc { get; init; }
}
