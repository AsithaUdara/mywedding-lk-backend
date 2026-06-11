using MediatR;
using MyWedding.Collaboration.Application.Features.AuditLog;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.AuditLog.Queries.GetAuditLogByEventId
{
    public class GetAuditLogByEventIdQueryHandler : IRequestHandler<GetAuditLogByEventIdQuery, IEnumerable<AuditLogItemDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IWeddingEventRepository _eventRepository;

        public GetAuditLogByEventIdQueryHandler(
            IAuditLogRepository auditLogRepository,
            IEventOrganizerRepository organizerRepository,
            IWeddingEventRepository eventRepository)
        {
            _auditLogRepository = auditLogRepository;
            _organizerRepository = organizerRepository;
            _eventRepository = eventRepository;
        }

        public async Task<IEnumerable<AuditLogItemDto>> Handle(GetAuditLogByEventIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new ForbiddenAccessException("You do not have access to this event activity.");
            }

            var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);

            var organizer = await _organizerRepository.GetOrganizerAsync(
                request.EventId,
                request.UserId,
                cancellationToken);

            if (organizer is null)
            {
                var canAccess = weddingEvent is not null
                    && (weddingEvent.ManagingPlannerId == request.UserId
                        || weddingEvent.CreatedById == request.UserId
                        || await _eventRepository.IsManagedByPlannerAsync(
                            request.EventId,
                            request.UserId,
                            cancellationToken));

                if (!canAccess)
                {
                    throw new ForbiddenAccessException("You do not have access to this event activity.");
                }
            }

            var items = await _auditLogRepository.GetByEventIdAsync(request.EventId, cancellationToken);
            var organizers = (await _organizerRepository.GetOrganizersByEventIdAsync(request.EventId, cancellationToken))
                .GroupBy(o => o.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            return items
                .OrderByDescending(i => i.TimestampUtc)
                .Select(i =>
                {
                    organizers.TryGetValue(i.ActorId, out var organizer);
                    var displayName = ActivityActorDisplayName.Resolve(
                        i.Actor?.FirstName,
                        i.Actor?.LastName,
                        i.Actor?.Email,
                        i.ActorId,
                        weddingEvent?.ManagingPlannerId,
                        organizer?.Role);

                    return new AuditLogItemDto(
                        i.Id,
                        i.ActionType,
                        i.Content,
                        i.MetadataJson,
                        i.TimestampUtc,
                        i.Actor!.Id,
                        i.Actor.FirstName,
                        i.Actor.LastName,
                        displayName);
                });
        }
    }
}
