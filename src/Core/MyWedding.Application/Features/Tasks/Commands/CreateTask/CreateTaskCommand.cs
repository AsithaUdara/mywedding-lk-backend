// File: src/Core/MyWedding.Application/Features/Tasks/Commands/CreateTask/CreateTaskCommand.cs
using MediatR;
using System;

namespace MyWedding.Application.Features.Tasks.Commands.CreateTask
{
    public class CreateTaskCommand : IRequest<Guid>
    {
        public Guid EventId { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime? DueDate { get; init; }
        public string? UserId { get; set; }
    }
}