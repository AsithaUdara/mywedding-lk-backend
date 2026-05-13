using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Collaboration.Application.Features.Polls.Commands.CreatePoll
{
    public class CreatePollCommand : IRequest<Guid>
    {
        public Guid EventId { get; init; }
        public string? UserId { get; set; } // Set from controller
        public required string Title { get; init; }
        public required List<string> Options { get; init; }
    }
}
