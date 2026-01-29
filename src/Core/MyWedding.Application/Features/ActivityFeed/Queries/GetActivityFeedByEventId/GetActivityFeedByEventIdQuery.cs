// File: src/Core/MyWedding.Application/Features/ActivityFeed/Queries/GetActivityFeedByEventId/GetActivityFeedByEventIdQuery.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Application.Features.ActivityFeed.Queries.GetActivityFeedByEventId
{
    public class GetActivityFeedByEventIdQuery : IRequest<IEnumerable<ActivityFeedItemDto>>
    {
        public Guid EventId { get; init; }
    }
}
