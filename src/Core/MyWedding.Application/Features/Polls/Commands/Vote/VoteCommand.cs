using MediatR;
using System;

namespace MyWedding.Application.Features.Polls.Commands.Vote
{
    public class VoteCommand : IRequest<Unit>
    {
        public Guid PollId { get; init; }
        public Guid OptionId { get; init; }
        public string? UserId { get; set; } // Set from controller
    }
}
