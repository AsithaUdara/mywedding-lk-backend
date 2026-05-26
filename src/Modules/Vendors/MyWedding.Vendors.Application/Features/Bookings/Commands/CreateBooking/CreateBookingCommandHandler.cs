using MediatR;
using MyWedding.Domain.Enums;

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IVendorBookingRepository bookingRepository,
            IWeddingEventRepository eventRepository,
            IVendorServiceRepository serviceRepository,
            IVendorRepository vendorRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _serviceRepository = serviceRepository;
            _vendorRepository = vendorRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to create booking for EventId: {EventId} by UserId: {UserId}", request.EventId, request.UserId);

            // --- STRICT SECURITY CHECK: Only Owner can book ---
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Event with ID {request.EventId} not found.");
            }

            // Only the creator (Owner) is allowed to initiate a booking
            if (weddingEvent.CreatedById != request.UserId)
            {
                _logger.LogWarning("Forbidden: UserId {UserId} attempted to book for EventId {EventId} but is NOT the owner.", request.UserId, request.EventId);
                throw new ForbiddenAccessException("Only the Event Owner has the authority to book vendors.");
            }
            // --- END OF SECURITY CHECK ---

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
    }
}
