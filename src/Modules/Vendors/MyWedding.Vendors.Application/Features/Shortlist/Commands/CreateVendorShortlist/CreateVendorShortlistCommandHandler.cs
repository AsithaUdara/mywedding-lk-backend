using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;

namespace MyWedding.Vendors.Application.Features.Shortlist.Commands.CreateVendorShortlist;

public class CreateVendorShortlistCommandHandler : IRequestHandler<CreateVendorShortlistCommand, IReadOnlyList<Guid>>
{
    private readonly IVendorShortlistRepository _shortlistRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IVendorServiceRepository _serviceRepository;
    private readonly IVendorBlockedDateRepository _blockedDateRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVendorShortlistCommandHandler(
        IVendorShortlistRepository shortlistRepository,
        IWeddingEventRepository eventRepository,
        IVendorServiceRepository serviceRepository,
        IVendorBlockedDateRepository blockedDateRepository,
        IEventOrganizerRepository organizerRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _shortlistRepository = shortlistRepository;
        _eventRepository = eventRepository;
        _serviceRepository = serviceRepository;
        _blockedDateRepository = blockedDateRepository;
        _organizerRepository = organizerRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Guid>> Handle(CreateVendorShortlistCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = ["At least one vendor service is required."]
            });
        }

        var canManage = await _eventRepository.IsManagedByPlannerAsync(
            request.EventId, request.PlannerId, cancellationToken);
        if (!canManage)
            throw new ForbiddenAccessException("Only the managing planner can create a vendor shortlist.");

        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
            throw new NotFoundException("Event", request.EventId);

        var now = DateTime.UtcNow;
        var status = request.SendToClient
            ? VendorShortlistItemStatus.SentToClient
            : VendorShortlistItemStatus.Draft;

        var createdIds = new List<Guid>();

        foreach (var item in request.Items)
        {
            var service = await _serviceRepository.GetByIdAsync(item.VendorServiceId, cancellationToken);
            if (service is null)
                throw new NotFoundException("VendorService", item.VendorServiceId);

            if (!service.IsActive)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["vendorServiceId"] = [$"Service {item.VendorServiceId} is not available."]
                });
            }

            var weddingDate = weddingEvent.EventDate.Date;
            var blocked = await _blockedDateRepository.GetByVendorAndDateAsync(
                service.VendorId,
                weddingDate,
                cancellationToken);

            if (blocked is not null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["vendorServiceId"] =
                    [
                        $"Vendor is unavailable on the wedding date ({weddingDate:yyyy-MM-dd})."
                    ]
                });
            }

            var amount = item.ProposedAmount > 0 ? item.ProposedAmount : service.BasePrice;

            var shortlistItem = new VendorShortlistItem
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                VendorServiceId = item.VendorServiceId,
                PlannerId = request.PlannerId,
                CategoryLabel = item.CategoryLabel,
                PlannerNotes = item.PlannerNotes,
                ProposedAmount = amount,
                ServiceDate = item.ServiceDate ?? weddingEvent.EventDate,
                Status = status,
                SentToClientAt = request.SendToClient ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _shortlistRepository.AddAsync(shortlistItem, cancellationToken);
            createdIds.Add(shortlistItem.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.SendToClient)
            await NotifyClientsAsync(request.EventId, weddingEvent.EventName, createdIds.Count, cancellationToken);

        return createdIds;
    }

    private async Task NotifyClientsAsync(
        Guid eventId,
        string eventName,
        int itemCount,
        CancellationToken cancellationToken)
    {
        var organizers = await _organizerRepository.GetOrganizersByEventIdAsync(eventId, cancellationToken);
        foreach (var org in organizers.Where(o =>
                     o.PermissionLevel != PermissionLevel.Viewer &&
                     o.Role != OrganizerRole.Planner))
        {
            await _notificationService.NotifyProposalReceivedAsync(
                org.UserId,
                new
                {
                    eventId,
                    eventName,
                    itemCount,
                    message = "Your planner sent vendor proposals for your review."
                },
                cancellationToken);
        }
    }
}
