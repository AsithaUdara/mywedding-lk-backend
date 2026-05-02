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
    /// <summary>
    /// Controller for managing event-related tasks and their statuses.
    /// </summary>
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Retrieves all tasks for a specific wedding event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the wedding event.</param>
        /// <returns>A list of event tasks.</returns>
        [HttpGet("api/events/{eventId:guid}/tasks")]
        public async Task<IActionResult> GetTasksForEvent(Guid eventId)
        {
            var query = new GetTasksByEventIdQuery { EventId = eventId };
            var tasks = await _mediator.Send(query);
            return Ok(tasks);
        }

        /// <summary>
        /// Creates a new task within a specific wedding event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the wedding event.</param>
        /// <param name="request">The task details.</param>
        /// <returns>The ID of the created task.</returns>
        [HttpPost("api/events/{eventId:guid}/tasks")]
        public async Task<IActionResult> CreateTask(Guid eventId, [FromBody] CreateTaskRequest request)
        {
            var userId = GetUserId();
            var command = new CreateTaskCommand
            {
                EventId = eventId,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                UserId = userId
            };

            var taskId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetTasksForEvent), new { eventId = eventId }, new { TaskId = taskId });
        }
        
        /// <summary>
        /// Updates the status of an existing task.
        /// </summary>
        /// <param name="taskId">The ID of the task to update.</param>
        /// <param name="request">The new status Details.</param>
        /// <returns>NoContent if successful.</returns>
        [HttpPut("api/tasks/{taskId:guid}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

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

    /// <summary>Request DTO for creating a new task.</summary>
    public record CreateTaskRequest(string Title, string? Description, DateTime? DueDate);

    /// <summary>Request DTO for updating a task's status.</summary>
    public record UpdateTaskStatusRequest(MyWedding.Domain.Enums.TaskStatus NewStatus);
}
