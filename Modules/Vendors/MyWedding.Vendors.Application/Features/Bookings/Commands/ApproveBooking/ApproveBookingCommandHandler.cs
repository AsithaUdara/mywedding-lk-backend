using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.ApproveBooking;

/// <summary>
/// Client/planner approves a vendor booking request (triggers notifyBookingApproved).
/// </summary>
public class ApproveBookingCommandHandler : IRequestHandler<ApproveBookingCommand, bool>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveBookingCommandHandler(
        IVendorBookingRepository bookingRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ApproveBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException("Booking", request.BookingId);
        }

        var organizer = await _organizerRepository.GetOrganizerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);
        if (organizer is null || organizer.PermissionLevel == PermissionLevel.Viewer)
        {
            throw new ForbiddenAccessException("You do not have permission to approve this booking.");
        }

        booking.Status = BookingStatus.AwaitingPayment;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            await _notificationService.NotifyBookingApprovedAsync(
                weddingEvent.ManagingPlannerId,
                new
                {
                    bookingId = booking.Id,
                    eventId = booking.EventId,
                    message = "A client approved a vendor for this event."
                },
                cancellationToken);
        }

        return true;
    }
}
