// File: src/Presentation/MyWedding.API/Controllers/PollsController.cs
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

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // GET /api/polls/event/{eventId}
        [HttpGet("event/{eventId:guid}")]
        public async Task<IActionResult> GetPollsForEvent(Guid eventId)
        {
            try 
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // POST /api/polls
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] CreatePollRequest request)
        {
            try 
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // POST /api/polls/{pollId}/vote
        [HttpPost("{pollId:guid}/vote")]
        public async Task<IActionResult> Vote(Guid pollId, [FromBody] VoteRequest request)
        {
            try 
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                // Check if user already voted in this poll
                var existingVote = await _context.PollVotes
                    .FirstOrDefaultAsync(v => v.PollOption!.PollId == pollId && v.UserId == userId);

                if (existingVote != null)
                {
                    _context.PollVotes.Remove(existingVote); // Remove old vote (simple toggle or update logic)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }

    public record CreatePollRequest(Guid EventId, string Title, List<string> Options);
    public record VoteRequest(Guid OptionId);
}
