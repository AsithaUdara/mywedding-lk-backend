using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.ApproveVendorFromShortlist;

public class ApproveVendorFromShortlistCommandHandler : IRequestHandler<ApproveVendorFromShortlistCommand, Guid>
{
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveVendorFromShortlistCommandHandler(
        IVendorShortlistRepository shortlistRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _shortlistRepository = shortlistRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(ApproveVendorFromShortlistCommand request, CancellationToken cancellationToken)
    {
        var organizer = await _organizerRepository.GetOrganizerAsync(
            request.EventId, request.ClientUserId, cancellationToken);
        if (organizer is null || organizer.PermissionLevel == PermissionLevel.Viewer)
            throw new ForbiddenAccessException("You do not have permission to approve vendors for this event.");

        var item = await _shortlistRepository.GetByIdAsync(request.ShortlistItemId, cancellationToken);
        if (item is null || item.EventId != request.EventId)
            throw new NotFoundException("VendorShortlistItem", request.ShortlistItemId);

        if (item.Status != VendorShortlistItemStatus.SentToClient)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["This vendor proposal is not available for client review."]
            });
        }

        var now = DateTime.UtcNow;
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);

        if (request.Reject)
        {
            item.Status = VendorShortlistItemStatus.ClientRejected;
            item.UpdatedAt = now;
            _shortlistRepository.Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
            {
                await _notificationService.NotifyVendorBookingDeclinedForPlannerAsync(
                    weddingEvent.ManagingPlannerId,
                    new
                    {
                        eventId = request.EventId,
                        shortlistItemId = item.Id,
                        categoryLabel = item.CategoryLabel,
                        status = VendorShortlistItemStatus.ClientRejected.ToString(),
                        message =
                            "Your client declined a vendor proposal. Review the shortlist and send updated options."
                    },
                    cancellationToken);
            }

            return Guid.Empty;
        }

        item.Status = VendorShortlistItemStatus.ClientApproved;
        item.ClientApprovedByUserId = request.ClientUserId;
        item.ClientApprovedAt = now;
        item.UpdatedAt = now;
        _shortlistRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(weddingEvent?.ManagingPlannerId))
        {
            await _notificationService.NotifyBookingApprovedAsync(
                weddingEvent.ManagingPlannerId,
                new
                {
                    eventId = request.EventId,
                    shortlistItemId = item.Id,
                    message = "Your client approved a vendor from the shortlist."
                },
                cancellationToken);
        }

        return item.Id;
    }
}
