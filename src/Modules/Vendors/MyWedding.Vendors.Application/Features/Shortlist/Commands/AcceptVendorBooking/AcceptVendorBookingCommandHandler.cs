using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.AcceptVendorBooking;

public class AcceptVendorBookingCommandHandler : IRequestHandler<AcceptVendorBookingCommand, Unit>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptVendorBookingCommandHandler(
        IVendorBookingRepository bookingRepository,
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
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

        booking.Status = BookingStatus.Confirmed;
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
            status = BookingStatus.Confirmed.ToString(),
            message = "Your vendor booking has been confirmed."
        };

        await _notificationService.NotifyBookingConfirmedAsync(
            booking.BookedById, payload, cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            await _notificationService.NotifyBookingConfirmedForPlannerAsync(
                weddingEvent.ManagingPlannerId,
                new
                {
                    bookingId = booking.Id,
                    eventId = booking.EventId,
                    status = BookingStatus.Confirmed.ToString(),
                    message = "A vendor has confirmed a booking for your client event."
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}
