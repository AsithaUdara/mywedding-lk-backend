// File: src/Core/MyWedding.Application/Features/EventOrganizers/Queries/GetOrganizersByEventId/GetOrganizersByEventIdQueryHandler.cs
using MediatR;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.EventOrganizers.Queries.GetOrganizersByEventId
{
    public class GetOrganizersByEventIdQueryHandler : IRequestHandler<GetOrganizersByEventIdQuery, IEnumerable<OrganizerDto>>
    {
        private readonly IEventOrganizerRepository _organizerRepository;

        public GetOrganizersByEventIdQueryHandler(IEventOrganizerRepository organizerRepository)
        {
            _organizerRepository = organizerRepository;
        }

        public async Task<IEnumerable<OrganizerDto>> Handle(GetOrganizersByEventIdQuery request, CancellationToken cancellationToken)
        {
            var organizers = await _organizerRepository.GetOrganizersByEventIdAsync(request.EventId, cancellationToken);

            // Map the list of entities to a list of DTOs, including user details
            return organizers.Select(o => new OrganizerDto
            {
                UserId = o.User!.Id,
                Email = o.User.Email,
                FirstName = o.User.FirstName,
                LastName = o.User.LastName,
                Role = o.Role.ToString(),
                PermissionLevel = o.PermissionLevel.ToString()
            });
        }
    }
}
