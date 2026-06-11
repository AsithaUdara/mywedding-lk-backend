// File: src/Core/MyWedding.Application/Features/Tasks/Queries/GetTasksByEventId/TaskDto.cs
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetTasksByEventId
{
    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        DateTime? StartDate,
        DateTime? DueDate,
        Guid? DependsOnTaskId,
        string? AssignedToUserId,
        DateTime CreatedAt
    );
}
