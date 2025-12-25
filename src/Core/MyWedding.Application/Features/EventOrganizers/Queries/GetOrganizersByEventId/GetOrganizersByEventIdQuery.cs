// File: src/Core/MyWedding.Application/Features/EventOrganizers/Queries/GetOrganizersByEventId/GetOrganizersByEventIdQuery.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Application.Features.EventOrganizers.Queries.GetOrganizersByEventId
{
    public class GetOrganizersByEventIdQuery : IRequest<IEnumerable<OrganizerDto>>
    {
        public Guid EventId { get; init; }
    }
}
