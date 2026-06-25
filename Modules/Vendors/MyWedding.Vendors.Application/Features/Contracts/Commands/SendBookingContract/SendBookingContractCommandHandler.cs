using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Contracts.Commands.SendBookingContract;

public class SendBookingContractCommandHandler : IRequestHandler<SendBookingContractCommand, SendBookingContractResult>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IBookingContractRepository _contractRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public SendBookingContractCommandHandler(
        IVendorBookingRepository bookingRepository,
        IBookingContractRepository contractRepository,
        IWeddingEventRepository eventRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _contractRepository = contractRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SendBookingContractResult> Handle(
        SendBookingContractCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException("VendorBooking", request.BookingId);

        if (booking.VendorService?.Vendor?.UserId != request.VendorUserId)
            throw new ForbiddenAccessException();

        if (booking.Status is not (BookingStatus.AwaitingPayment or BookingStatus.ContractSigned))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Contract can only be sent while the booking is awaiting payment."]
            });
        }

        var contract = await _contractRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        if (contract is null || string.IsNullOrWhiteSpace(contract.ContractFileUrl))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["contract"] = ["Upload or generate a contract PDF before sending it to the client."]
            });
        }

        if (contract.VendorSignedAt is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["contract"] = ["This contract was already sent to the client."]
            });
        }

        var sentAt = DateTime.UtcNow;
        contract.VendorSignedAt = sentAt;
        _contractRepository.Update(contract);

        if (booking.Status == BookingStatus.ContractSigned)
        {
            booking.Status = BookingStatus.AwaitingPayment;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var eventName = (await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken))?.EventName
            ?? "your wedding";

        await _notificationService.NotifyBookingConfirmedAsync(
            booking.BookedById,
            new
            {
                bookingId = booking.Id,
                eventId = booking.EventId,
                action = "signContract",
                message = $"{booking.VendorService?.Vendor?.BusinessName ?? "Your vendor"} sent the contract for {eventName}. Review and sign before paying the deposit."
            },
            cancellationToken);

        return new SendBookingContractResult
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            SentToClientAtUtc = sentAt
        };
    }
}
