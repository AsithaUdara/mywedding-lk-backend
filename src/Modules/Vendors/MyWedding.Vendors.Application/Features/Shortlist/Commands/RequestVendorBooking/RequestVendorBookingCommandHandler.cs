using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.RequestVendorBooking;

public class RequestVendorBookingCommandHandler : IRequestHandler<RequestVendorBookingCommand, Guid>
{
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IVendorServiceRepository _serviceRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public RequestVendorBookingCommandHandler(
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        IVendorBookingRepository bookingRepository,
        IVendorServiceRepository serviceRepository,
        IVendorRepository vendorRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _bookingRepository = bookingRepository;
        _serviceRepository = serviceRepository;
        _vendorRepository = vendorRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RequestVendorBookingCommand request, CancellationToken cancellationToken)
    {
        var isPlanner = await _eventRepository.IsManagedByPlannerAsync(
            request.EventId, request.UserId, cancellationToken);
        var organizer = await _organizerRepository.GetOrganizerAsync(
            request.EventId, request.UserId, cancellationToken);
        var isClientEditor = organizer is not null && organizer.PermissionLevel != PermissionLevel.Viewer;

        if (!isPlanner && !isClientEditor)
            throw new ForbiddenAccessException();

        var item = await _shortlistRepository.GetByIdAsync(request.ShortlistItemId, cancellationToken);
        if (item is null || item.EventId != request.EventId)
            throw new NotFoundException("VendorShortlistItem", request.ShortlistItemId);

        if (item.Status is not VendorShortlistItemStatus.ClientApproved
            and not VendorShortlistItemStatus.BookingRequested)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Vendor must be client-approved before a booking can be requested."]
            });
        }

        var service = await _serviceRepository.GetByIdAsync(item.VendorServiceId, cancellationToken);
        if (service is null)
            throw new NotFoundException("VendorService", item.VendorServiceId);

        var vendor = await _vendorRepository.GetByIdAsync(service.VendorId, cancellationToken);
        if (vendor is null)
            throw new NotFoundException("Vendor", service.VendorId);

        var now = DateTime.UtcNow;
        Guid bookingId;

        if (item.VendorBookingId is Guid existingId)
        {
            var existing = await _bookingRepository.GetByIdAsync(existingId, cancellationToken);
            if (existing is null)
                throw new NotFoundException("VendorBooking", existingId);

            bookingId = existing.Id;
            if (existing.Status != BookingStatus.Requested)
            {
                existing.Status = BookingStatus.Requested;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var bookedBy = item.ClientApprovedByUserId ?? request.UserId;
            var booking = new VendorBooking
            {
                Id = Guid.NewGuid(),
                EventId = item.EventId,
                ServiceId = item.VendorServiceId,
                FinalAmount = item.ProposedAmount,
                ServiceDate = item.ServiceDate ?? DateTime.UtcNow.Date.AddMonths(6),
                BookedById = bookedBy,
                Status = BookingStatus.Requested,
                CreatedAt = now
            };
            await _bookingRepository.AddAsync(booking, cancellationToken);
            bookingId = booking.Id;
            item.VendorBookingId = bookingId;
        }

        item.Status = VendorShortlistItemStatus.BookingRequested;
        item.UpdatedAt = now;
        _shortlistRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyNewInquiryAsync(
            vendor.UserId,
            new
            {
                bookingId,
                eventId = item.EventId,
                serviceId = item.VendorServiceId,
                message = "You have a new booking request for this event."
            },
            cancellationToken);

        return bookingId;
    }
}
