using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;




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
            var query = new GetTasksByEventIdQuery { EventId = eventId, UserId = GetUserId() };
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
                StartDate = request.StartDate,
                DependsOnTaskId = request.DependsOnTaskId,
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

        /// <summary>
        /// Updates task schedule fields (Gantt drag-and-drop).
        /// </summary>
        [HttpPatch("api/events/{eventId:guid}/tasks/{taskId:guid}")]
        public async Task<IActionResult> UpdateTaskSchedule(
            Guid eventId,
            Guid taskId,
            [FromBody] UpdateTaskScheduleRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var command = new UpdateTaskScheduleCommand
            {
                EventId = eventId,
                TaskId = taskId,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                DependsOnTaskId = request.DependsOnTaskId,
                UpdateDependency = request.UpdateDependency,
                UserId = userId
            };

            await _mediator.Send(command);

            return NoContent();
        }

        /// <summary>
        /// Realigns incomplete template tasks from today forward (fixes overdue schedules for mid-planning weddings).
        /// </summary>
        [HttpPost("api/events/{eventId:guid}/tasks/realign-schedule")]
        public async Task<IActionResult> RealignTaskSchedule(Guid eventId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new RealignEventTaskScheduleCommand
            {
                EventId = eventId,
                UserId = userId
            });

            return Ok(new
            {
                message = "Task schedule realigned from today through wedding day.",
                tasksUpdated = result.TasksUpdated,
                tasksSkipped = result.TasksSkipped
            });
        }
    }

    /// <summary>Request DTO for creating a new task.</summary>
    public record CreateTaskRequest(string Title, string? Description, DateTime? DueDate, DateTime? StartDate, Guid? DependsOnTaskId);

    /// <summary>Request DTO for updating a task's status.</summary>
    public record UpdateTaskStatusRequest(MyWedding.Domain.Enums.TaskStatus NewStatus);

    /// <summary>Request DTO for updating task schedule (Gantt).</summary>
    public record UpdateTaskScheduleRequest(
        DateTime? StartDate,
        DateTime? DueDate,
        Guid? DependsOnTaskId,
        bool UpdateDependency = false);
}
