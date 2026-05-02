using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    /// <summary>
    /// Controller for managing team polls within wedding events.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PollsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediator _mediator;

        public PollsController(ApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Retrieves all active polls for a specific wedding event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the wedding event.</param>
        /// <returns>A list of polls with their options and vote counts.</returns>
        [HttpGet("event/{eventId:guid}")]
        public async Task<IActionResult> GetPollsForEvent(Guid eventId)
        {
            var polls = await _context.Polls
                .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
                .ThenInclude(v => v.User)
                .Where(p => p.EventId == eventId && p.IsActive)
                .Select(p => new {
                    p.Id,
                    p.Title,
                    p.CreatedById,
                    Options = p.Options.Select(o => new {
                        o.Id,
                        o.OptionText,
                        VoteCount = o.Votes.Count,
                        Voters = o.Votes.Select(v => new { id = v.UserId, name = v.User != null ? v.User.FirstName + " " + v.User.LastName : "Unknown" })
                    }),
                    HasVoted = p.Options.Any(o => o.Votes.Any(v => v.UserId == GetUserId()))
                })
                .ToListAsync();

            return Ok(polls);
        }

        /// <summary>
        /// Creates a new poll within an event.
        /// </summary>
        /// <param name="request">The poll details including title and options.</param>
        /// <returns>The ID of the newly created poll.</returns>
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] CreatePollRequest request)
        {
            var command = new MyWedding.Application.Features.Polls.Commands.CreatePoll.CreatePollCommand
            {
                EventId = request.EventId,
                Title = request.Title,
                Options = request.Options,
                UserId = GetUserId()
            };

            var pollId = await _mediator.Send(command);
            return Ok(pollId);
        }

        /// <summary>
        /// Casts or updates a vote in a specific poll.
        /// </summary>
        /// <param name="pollId">The ID of the poll.</param>
        /// <param name="request">The option being voted for.</param>
        /// <returns>Ok if successful.</returns>
        [HttpPost("{pollId:guid}/vote")]
        public async Task<IActionResult> Vote(Guid pollId, [FromBody] VoteRequest request)
        {
            var command = new MyWedding.Application.Features.Polls.Commands.Vote.VoteCommand
            {
                PollId = pollId,
                OptionId = request.OptionId,
                UserId = GetUserId()
            };

            await _mediator.Send(command);
            return Ok();
        }
    }

    /// <summary>Request DTO for creating a new poll.</summary>
    public record CreatePollRequest(Guid EventId, string Title, List<string> Options);

    /// <summary>Request DTO for voting on a poll option.</summary>
    public record VoteRequest(Guid OptionId);
}
