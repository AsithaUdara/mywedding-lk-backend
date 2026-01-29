// File: src/Core/MyWedding.Application/Features/ActivityFeed/Queries/GetActivityFeedByEventId/GetActivityFeedByEventIdQueryHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.ActivityFeed.Queries.GetActivityFeedByEventId
{
    public class GetActivityFeedByEventIdQueryHandler : IRequestHandler<GetActivityFeedByEventIdQuery, IEnumerable<ActivityFeedItemDto>>
    {
        private readonly IActivityFeedRepository _activityFeedRepository;

        public GetActivityFeedByEventIdQueryHandler(IActivityFeedRepository activityFeedRepository)
        {
            _activityFeedRepository = activityFeedRepository;
        }

        public async Task<IEnumerable<ActivityFeedItemDto>> Handle(GetActivityFeedByEventIdQuery request, CancellationToken cancellationToken)
        {
            var items = await _activityFeedRepository.GetByEventIdAsync(request.EventId, cancellationToken);

            return items
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new ActivityFeedItemDto(
                    i.Id,
                    i.ItemType.ToString(),
                    i.Content,
                    i.CreatedAt,
                    i.User!.Id,
                    i.User.FirstName,
                    i.User.LastName
                ));
        }
    }
}
