// File: src/Presentation/MyWedding.API/Controllers/TasksController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Tasks.Commands.CreateTask;
using MyWedding.Application.Features.Tasks.Commands.UpdateTaskStatus;
using MyWedding.Application.Features.Tasks.Queries.GetTasksByEventId;
using MyWedding.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Authorize] // All endpoints in this controller require authentication
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/events/{eventId}/tasks
        [HttpGet("api/events/{eventId:guid}/tasks")]
        public async Task<IActionResult> GetTasksForEvent(Guid eventId)
        {
            // TODO: Add security check to ensure user is an organizer of this event
            var query = new GetTasksByEventIdQuery { EventId = eventId };
            var tasks = await _mediator.Send(query);
            return Ok(tasks);
        }

        // POST /api/events/{eventId}/tasks
        [HttpPost("api/events/{eventId:guid}/tasks")]
        public async Task<IActionResult> CreateTask(Guid eventId, [FromBody] CreateTaskRequest request)
        {
            // TODO: Add security check to ensure user has 'Editor' or 'Owner' permissions
            var command = new CreateTaskCommand
            {
                EventId = eventId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate
            };

            var taskId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetTasksForEvent), new { eventId = eventId }, new { TaskId = taskId });
        }
        
        // PUT /api/tasks/{taskId}/status
        [HttpPut("api/tasks/{taskId:guid}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // TODO: Add security check to ensure user has 'Editor' or 'Owner' permissions
            var command = new UpdateTaskStatusCommand
            {
                TaskId = taskId,
                NewStatus = request.NewStatus,
                UserId = userId
            };

            await _mediator.Send(command);

            return NoContent();
        }
    }

    // --- DTOs for the request bodies ---
    public record CreateTaskRequest(string Title, string? Description, DateTime? DueDate);
    public record UpdateTaskStatusRequest(MyWedding.Domain.Enums.TaskStatus NewStatus);
}