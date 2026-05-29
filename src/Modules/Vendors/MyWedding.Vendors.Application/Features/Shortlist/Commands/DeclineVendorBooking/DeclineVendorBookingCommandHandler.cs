using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.DeclineVendorBooking;

public class DeclineVendorBookingCommandHandler : IRequestHandler<DeclineVendorBookingCommand, Unit>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public DeclineVendorBookingCommandHandler(
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

    public async Task<Unit> Handle(DeclineVendorBookingCommand request, CancellationToken cancellationToken)
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
                ["status"] = ["Only requested bookings can be declined by the vendor."]
            });
        }

        booking.Status = BookingStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var shortlistItems = await _shortlistRepository.GetByEventIdAsync(booking.EventId, cancellationToken);
        var linkedItem = shortlistItems.FirstOrDefault(i => i.VendorBookingId == booking.Id);
        if (linkedItem is not null)
        {
            var tracked = await _shortlistRepository.GetByIdAsync(linkedItem.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.Status = VendorShortlistItemStatus.Declined;
                tracked.UpdatedAt = DateTime.UtcNow;
                _shortlistRepository.Update(tracked);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var vendorName = booking.VendorService?.Vendor?.BusinessName ?? "A vendor";
        var clientPayload = new
        {
            bookingId = booking.Id,
            eventId = booking.EventId,
            status = VendorShortlistItemStatus.Declined.ToString(),
            message = $"{vendorName} declined your booking request. Your planner can suggest another vendor."
        };

        await _notificationService.NotifyBookingConfirmedAsync(
            booking.BookedById,
            clientPayload,
            cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            await _notificationService.NotifyVendorBookingDeclinedForPlannerAsync(
                weddingEvent.ManagingPlannerId,
                new
                {
                    bookingId = booking.Id,
                    eventId = booking.EventId,
                    vendorName,
                    status = VendorShortlistItemStatus.Declined.ToString(),
                    message = $"{vendorName} declined a booking request for {weddingEvent.EventName}."
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}
