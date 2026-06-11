using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.AcceptVendorBooking;

public class AcceptVendorBookingCommandHandler : IRequestHandler<AcceptVendorBookingCommand, Unit>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptVendorBookingCommandHandler(
        IVendorBookingRepository bookingRepository,
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository,
        INotificationService notificationService,
        IEmailService emailService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _emailService = emailService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AcceptVendorBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            throw new NotFoundException("VendorBooking", request.BookingId);

        if (booking.VendorService?.Vendor?.UserId != request.VendorUserId)
            throw new ForbiddenAccessException();

        if (booking.Status != BookingStatus.Requested && booking.Status != BookingStatus.Pending)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Only requested bookings can be accepted by the vendor."]
            });
        }

        booking.Status = BookingStatus.AwaitingPayment;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var shortlistItems = await _shortlistRepository.GetByEventIdAsync(booking.EventId, cancellationToken);
        var linkedItem = shortlistItems.FirstOrDefault(i => i.VendorBookingId == booking.Id);
        if (linkedItem is not null)
        {
            var tracked = await _shortlistRepository.GetByIdAsync(linkedItem.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.Status = VendorShortlistItemStatus.BookingAccepted;
                tracked.UpdatedAt = DateTime.UtcNow;
                _shortlistRepository.Update(tracked);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var payload = new
        {
            bookingId = booking.Id,
            eventId = booking.EventId,
            status = BookingStatus.AwaitingPayment.ToString(),
            action = "awaitContract",
            message = "The vendor accepted your request. They will send a contract for you to sign before the deposit."
        };

        await _notificationService.NotifyBookingConfirmedAsync(
            booking.BookedById, payload, cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
        var client = await _userRepository.GetByIdAsync(booking.BookedById, cancellationToken);
        if (!string.IsNullOrWhiteSpace(client?.Email))
        {
            await _emailService.SendVendorBookingAcceptedAsync(
                new VendorBookingAcceptedEmailMessage(
                    client.Email,
                    weddingEvent?.EventName ?? "Your wedding",
                    booking.VendorService?.Vendor?.BusinessName ?? "Vendor",
                    booking.Id,
                    booking.EventId),
                cancellationToken);
        }

        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            await _notificationService.NotifyBookingConfirmedForPlannerAsync(
                weddingEvent.ManagingPlannerId,
                new
                {
                    bookingId = booking.Id,
                    eventId = booking.EventId,
                    status = BookingStatus.AwaitingPayment.ToString(),
                    message = "A vendor accepted a booking request — awaiting contract and client deposit."
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}
