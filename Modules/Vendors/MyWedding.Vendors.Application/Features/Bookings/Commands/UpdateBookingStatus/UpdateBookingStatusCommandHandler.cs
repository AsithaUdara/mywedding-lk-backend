using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.UpdateBookingStatus
{
    public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, bool>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBookingStatusCommandHandler(
            IVendorBookingRepository bookingRepository,
            IWeddingEventRepository eventRepository,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException("Booking", request.BookingId);
            }

            if (booking.VendorService?.Vendor?.UserId != request.VendorUserId)
            {
                throw new ForbiddenAccessException();
            }

            booking.Status = request.NewStatus;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.NewStatus == BookingStatus.Confirmed)
            {
                var payload = new
                {
                    bookingId = booking.Id,
                    eventId = booking.EventId,
                    status = request.NewStatus.ToString(),
                    message = "Your vendor booking has been confirmed."
                };

                await _notificationService.NotifyBookingConfirmedAsync(
                    booking.BookedById,
                    payload,
                    cancellationToken);

                var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(booking.EventId, cancellationToken);
                if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
                {
                    await _notificationService.NotifyBookingConfirmedForPlannerAsync(
                        weddingEvent.ManagingPlannerId,
                        new
                        {
                            bookingId = booking.Id,
                            eventId = booking.EventId,
                            status = request.NewStatus.ToString(),
                            message = "A vendor has confirmed a booking for your client event."
                        },
                        cancellationToken);
                }
            }

            return true;
        }
    }
}
