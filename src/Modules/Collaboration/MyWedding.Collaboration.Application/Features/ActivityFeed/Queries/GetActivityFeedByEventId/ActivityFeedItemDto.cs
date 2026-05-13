// File: src/Core/MyWedding.Application/Features/ActivityFeed/Queries/GetActivityFeedByEventId/ActivityFeedItemDto.cs
using System;

namespace MyWedding.Collaboration.Application.Features.ActivityFeed.Queries.GetActivityFeedByEventId
{
    public record ActivityFeedItemDto(
        Guid Id,
        string ItemType,
        string Content,
        DateTime CreatedAt,
        string UserId,
        string UserFirstName,
        string UserLastName
    );
}
