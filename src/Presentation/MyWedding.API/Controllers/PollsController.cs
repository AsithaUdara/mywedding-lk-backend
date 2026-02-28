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

        public PollsController(ApplicationDbContext context)
        {
            _context = context;
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
                .Where(p => p.EventId == eventId && p.IsActive)
                .Select(p => new {
                    p.Id,
                    p.Title,
                    p.CreatedById,
                    Options = p.Options.Select(o => new {
                        o.Id,
                        o.OptionText,
                        VoteCount = o.Votes.Count,
                        Voters = o.Votes.Select(v => v.UserId)
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
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                EventId = request.EventId,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Options = request.Options.Select(o => new PollOption { 
                    Id = Guid.NewGuid(), 
                    OptionText = o 
                }).ToList()
            };

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return Ok(poll.Id);
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
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Check if user already voted in this poll
            var existingVote = await _context.PollVotes
                .FirstOrDefaultAsync(v => v.PollOption!.PollId == pollId && v.UserId == userId);

            if (existingVote != null)
            {
                _context.PollVotes.Remove(existingVote); // Toggle logic: remove existing vote
            }

            var vote = new PollVote
            {
                Id = Guid.NewGuid(),
                PollOptionId = request.OptionId,
                UserId = userId,
                VotedAt = DateTime.UtcNow
            };

            _context.PollVotes.Add(vote);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    /// <summary>Request DTO for creating a new poll.</summary>
    public record CreatePollRequest(Guid EventId, string Title, List<string> Options);

    /// <summary>Request DTO for voting on a poll option.</summary>
    public record VoteRequest(Guid OptionId);
}
