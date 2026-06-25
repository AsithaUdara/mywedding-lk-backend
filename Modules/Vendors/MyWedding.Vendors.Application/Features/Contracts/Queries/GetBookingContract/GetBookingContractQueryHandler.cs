using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Application.Features.Contracts.Queries.GetBookingContract;

public class GetBookingContractQueryHandler : IRequestHandler<GetBookingContractQuery, BookingContractDto?>
{
    private readonly IVendorBookingRepository _bookingRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;

    public GetBookingContractQueryHandler(
        IVendorBookingRepository bookingRepository,
        IEventOrganizerRepository organizerRepository,
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _organizerRepository = organizerRepository;
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
    }

    public async Task<BookingContractDto?> Handle(
        GetBookingContractQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            return null;

        var isVendor = booking.VendorService?.Vendor?.UserId == request.UserId;
        var isBooker = booking.BookedById == request.UserId;
        var organizer = await _organizerRepository.GetOrganizerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);
        var isPlanner = await _eventRepository.IsManagedByPlannerAsync(
            booking.EventId,
            request.UserId,
            cancellationToken);

        if (!isVendor && !isBooker && organizer is null && !isPlanner)
            throw new ForbiddenAccessException();

        var requiresContract = await _shortlistRepository.ExistsForBookingIdAsync(
            booking.Id,
            cancellationToken);

        var contract = booking.BookingContract;
        var hasContract = !string.IsNullOrWhiteSpace(contract?.ContractFileUrl);
        var sentToClient = contract?.VendorSignedAt is not null;
        var isSigned = contract?.ClientSignedAt is not null
            || booking.Status == BookingStatus.ContractSigned;

        var canEditAsClient = isBooker
            || (organizer is not null && organizer.PermissionLevel != PermissionLevel.Viewer);

        var canPreview = hasContract && (isVendor || sentToClient || isPlanner || organizer is not null);
        var canSendToClient = isVendor && hasContract && !sentToClient && !isSigned;
        var canSign = canEditAsClient && hasContract && sentToClient && !isSigned
            && booking.Status is BookingStatus.AwaitingPayment or BookingStatus.ContractSigned;

        var canPayDeposit = canEditAsClient && booking.Status == BookingStatus.ContractSigned;
        if (!requiresContract)
        {
            canPayDeposit = canEditAsClient
                && booking.Status is BookingStatus.AwaitingPayment or BookingStatus.ContractSigned;
        }

        return new BookingContractDto(
            booking.Id,
            booking.EventId,
            sentToClient || isVendor ? contract?.ContractFileUrl : null,
            contract?.CreatedAt,
            contract?.VendorSignedAt,
            contract?.ClientSignedAt,
            booking.Status.ToString(),
            requiresContract,
            canPreview,
            canSendToClient,
            canSign,
            canPayDeposit);
    }
}
