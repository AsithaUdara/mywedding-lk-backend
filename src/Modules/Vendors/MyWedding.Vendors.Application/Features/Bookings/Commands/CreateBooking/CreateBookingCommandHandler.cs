using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IVendorBookingRepository bookingRepository,
            IWeddingEventRepository eventRepository,
            IVendorServiceRepository serviceRepository,
            IVendorRepository vendorRepository,
            IEventOrganizerRepository organizerRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _serviceRepository = serviceRepository;
            _vendorRepository = vendorRepository;
            _organizerRepository = organizerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to create booking for EventId: {EventId} by UserId: {UserId}", request.EventId, request.UserId);

            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Event with ID {request.EventId} not found.");
            }

            if (!await CanUserBookForEventAsync(weddingEvent.CreatedById, request.EventId, request.UserId, cancellationToken))
            {
                _logger.LogWarning(
                    "Forbidden: UserId {UserId} cannot book for EventId {EventId} (not owner/editor).",
                    request.UserId,
                    request.EventId);
                throw new ForbiddenAccessException(
                    "You do not have permission to book vendors for this event. Only the event owner or members with Editor access can book.");
            }

            var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
            if (service is null)
            {
                throw new NotFoundException($"Service with ID {request.ServiceId} not found.");
            }

            if (!service.IsActive)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["serviceId"] = ["This service is not available for booking."]
                });
            }

            var vendor = await _vendorRepository.GetByIdAsync(service.VendorId, cancellationToken);
            if (vendor is null)
            {
                throw new NotFoundException($"Vendor for service {request.ServiceId} not found.");
            }

            if (vendor.VerificationStatus != VerificationStatus.Verified)
            {
                throw new ForbiddenAccessException("This vendor is not verified and cannot accept bookings.");
            }

            if (request.FinalAmount < service.BasePrice)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["finalAmount"] = [$"Booking amount must be at least LKR {service.BasePrice:F2}."]
                });
            }

            var newBooking = new VendorBooking
            {
                Id = System.Guid.NewGuid(),
                EventId = request.EventId,
                ServiceId = request.ServiceId,
                FinalAmount = request.FinalAmount,
                ServiceDate = request.ServiceDate,
                BookedById = request.UserId,
                Status = Domain.Enums.BookingStatus.Requested,
                CreatedAt = System.DateTime.UtcNow
            };

            await _bookingRepository.AddAsync(newBooking, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully created BookingId: {BookingId} for EventId: {EventId}", newBooking.Id, request.EventId);

            return newBooking.Id;
        }

        private async Task<bool> CanUserBookForEventAsync(
            string createdById,
            Guid eventId,
            string userId,
            CancellationToken cancellationToken)
        {
            if (createdById == userId)
                return true;

            var organizer = await _organizerRepository.GetOrganizerAsync(eventId, userId, cancellationToken);
            return organizer is not null && organizer.PermissionLevel != PermissionLevel.Viewer;
        }
    }
}
