using MediatR;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.SendShortlistToClient;

public class SendShortlistToClientCommandHandler : IRequestHandler<SendShortlistToClientCommand, Unit>
{
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public SendShortlistToClientCommandHandler(
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(SendShortlistToClientCommand request, CancellationToken cancellationToken)
    {
        if (!await _eventRepository.IsManagedByPlannerAsync(request.EventId, request.PlannerId, cancellationToken))
            throw new ForbiddenAccessException();

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
            throw new NotFoundException("Event", request.EventId);

        var allItems = await _shortlistRepository.GetByEventIdAsync(request.EventId, cancellationToken);
        var targetIds = request.ItemIds is { Count: > 0 }
            ? request.ItemIds
            : allItems.Where(i => i.Status == VendorShortlistItemStatus.Draft).Select(i => i.Id).ToList();

        if (targetIds.Count == 0)
            return Unit.Value;

        var now = DateTime.UtcNow;
        var sentCount = 0;
        foreach (var id in targetIds)
        {
            var item = await _shortlistRepository.GetByIdAsync(id, cancellationToken);
            if (item is null || item.EventId != request.EventId || item.Status != VendorShortlistItemStatus.Draft)
                continue;

            item.Status = VendorShortlistItemStatus.SentToClient;
            item.SentToClientAt = now;
            item.UpdatedAt = now;
            _shortlistRepository.Update(item);
            sentCount++;
        }

        if (sentCount == 0)
            return Unit.Value;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var organizers = await _organizerRepository.GetOrganizersByEventIdAsync(request.EventId, cancellationToken);
        foreach (var org in organizers.Where(o =>
                     o.PermissionLevel != PermissionLevel.Viewer &&
                     o.Role != OrganizerRole.Planner))
        {
            await _notificationService.NotifyProposalReceivedAsync(
                org.UserId,
                new
                {
                    eventId = request.EventId,
                    eventName = weddingEvent.EventName,
                    itemCount = sentCount,
                    message = "Your planner sent vendor proposals for your review."
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}
