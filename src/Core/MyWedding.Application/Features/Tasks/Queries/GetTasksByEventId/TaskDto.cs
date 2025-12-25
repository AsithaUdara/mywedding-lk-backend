// File: src/Core/MyWedding.Application/Features/Tasks/Queries/GetTasksByEventId/TaskDto.cs
using System;
using MyWedding.Domain.Enums;

namespace MyWedding.Application.Features.Tasks.Queries.GetTasksByEventId
{
    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        DateTime? DueDate
    );
}