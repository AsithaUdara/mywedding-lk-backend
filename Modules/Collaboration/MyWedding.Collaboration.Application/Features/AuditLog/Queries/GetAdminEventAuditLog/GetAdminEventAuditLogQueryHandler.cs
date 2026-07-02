using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.ReadModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAdminEventAuditLog;

public class GetAdminEventAuditLogQueryHandler
    : IRequestHandler<GetAdminEventAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;

    public GetAdminEventAuditLogQueryHandler(
        IAuditLogRepository auditLogRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository)
    {
        _auditLogRepository = auditLogRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
    }

    public async Task<PagedResult<AuditLogEntryDto>> Handle(
        GetAdminEventAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            return new PagedResult<AuditLogEntryDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = 0,
            };
        }

        var page = await _auditLogRepository.GetByEventIdPagedAsync(
            request.EventId,
            request.Search,
            request.ActionType,
            request.Page,
            request.PageSize,
            cancellationToken);

        var organizers = AuditLogDtoMapper.IndexOrganizers(
            await _organizerRepository.GetOrganizersByEventIdAsync(request.EventId, cancellationToken));

        var items = page.Items
            .Select(item =>
            {
                var dto = AuditLogDtoMapper.Map(item, organizers, weddingEvent);
                return new AuditLogEntryDto
                {
                    Id = dto.Id,
                    ActionType = dto.ActionType,
                    Content = dto.Content,
                    MetadataJson = dto.MetadataJson,
                    TimestampUtc = dto.TimestampUtc,
                    ActorId = dto.ActorId,
                    ActorFirstName = dto.ActorFirstName,
                    ActorLastName = dto.ActorLastName,
                    ActorDisplayName = dto.ActorDisplayName,
                };
            })
            .ToList();

        return new PagedResult<AuditLogEntryDto>
        {
            Items = items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }
}
