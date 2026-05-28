using MediatR;
using System;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SignBookingContract;

public class SignBookingContractCommand : IRequest<SignBookingContractResult>
{
    public Guid BookingId { get; init; }
    public required string UserId { get; init; }
    public required string SignerName { get; init; }
    public required string ClientIpAddress { get; init; }
    public string? ContractFileUrl { get; init; }
}

public class SignBookingContractResult
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public DateTime SignedAtUtc { get; init; }
    public required string PdfContentHash { get; init; }
    public required string Status { get; init; }
}
