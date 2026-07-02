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
        /// <summary>
        /// Initializes a new instance of the <see cref="TasksController"/> class.
        /// </summary>
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
        /// <response code="200">Tasks returned successfully.</response>
        [HttpGet("api/events/{eventId:guid}/tasks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        /// <response code="201">Task created.</response>
        [HttpPost("api/events/{eventId:guid}/tasks")]
        [ProducesResponseType(StatusCodes.Status201Created)]
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
        /// <response code="204">Status updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPut("api/tasks/{taskId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// Assigns or clears the owner of a task (must be an event team member).
        /// </summary>
        /// <response code="204">Assignment updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPut("api/tasks/{taskId:guid}/assign")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AssignTask(Guid taskId, [FromBody] AssignTaskRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _mediator.Send(new AssignTaskCommand
            {
                TaskId = taskId,
                AssignedToUserId = request.AssignedToUserId,
                UserId = userId
            });

            return NoContent();
        }

        /// <summary>
        /// Updates task details (title, dates, status, dependency).
        /// </summary>
        /// <response code="204">Task updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPut("api/events/{eventId:guid}/tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateTask(
            Guid eventId,
            Guid taskId,
            [FromBody] UpdateTaskRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _mediator.Send(new UpdateTaskCommand
            {
                EventId = eventId,
                TaskId = taskId,
                Title = request.Title,
                Description = request.Description,
                Status = request.Status,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                DependsOnTaskId = request.DependsOnTaskId,
                UpdateDependency = request.UpdateDependency,
                UserId = userId
            });

            return NoContent();
        }

        /// <summary>
        /// Deletes a task from the event checklist.
        /// </summary>
        /// <response code="204">Task deleted.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpDelete("api/events/{eventId:guid}/tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteTask(Guid eventId, Guid taskId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _mediator.Send(new DeleteTaskCommand
            {
                EventId = eventId,
                TaskId = taskId,
                UserId = userId
            });

            return NoContent();
        }

        /// <summary>
        /// Updates task schedule fields (Gantt drag-and-drop).
        /// </summary>
        /// <response code="204">Schedule updated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPatch("api/events/{eventId:guid}/tasks/{taskId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// Returns a preview of the checklist plan before applying it to the event.
        /// </summary>
        /// <response code="200">Checklist preview returned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpGet("api/events/{eventId:guid}/checklist-preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetChecklistPreview(Guid eventId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preview = await _mediator.Send(new GetChecklistPlanPreviewQuery
            {
                EventId = eventId,
                UserId = userId
            });

            return Ok(preview);
        }

        /// <summary>
        /// Generates the full wedding planning checklist on the Gantt (after discovery phase).
        /// </summary>
        /// <response code="200">Checklist generated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("api/events/{eventId:guid}/tasks/generate-checklist")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateFullChecklist(
            Guid eventId,
            [FromBody] ApplyChecklistPlanRequest? request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (request?.ExcludeTemplateTitles is { Count: > 0 } || request?.AdditionalTasks is { Count: > 0 })
            {
                var personalized = await _mediator.Send(new ApplyPersonalizedChecklistCommand
                {
                    EventId = eventId,
                    UserId = userId,
                    ExcludeTemplateTitles = request.ExcludeTemplateTitles ?? [],
                    AdditionalTasks = (request.AdditionalTasks ?? []).Select(t =>
                        new AdditionalChecklistTask(t.Title, t.Description, t.StartDate, t.DueDate)).ToList(),
                    MarkBriefComplete = request.MarkBriefComplete
                });

                return Ok(new
                {
                    personalized.Message,
                    tasksCreated = personalized.TasksCreated,
                    taskPlanPhase = personalized.TaskPlanPhase
                });
            }

            var command = new GenerateTaskTemplateCommand
            {
                EventId = eventId,
                UserId = userId,
                SkipIfTasksExist = false
            };

            var tasksCreated = await _mediator.Send(command);

            return Ok(new
            {
                message = tasksCreated > 0
                    ? $"Generated {tasksCreated} planning tasks on your master checklist."
                    : "Master checklist was already generated for this event.",
                tasksCreated,
                taskPlanPhase = "Full"
            });
        }

        /// <summary>
        /// Generates discovery-phase tasks for a new event (before full checklist).
        /// </summary>
        /// <response code="200">Discovery tasks generated.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("api/events/{eventId:guid}/tasks/generate-discovery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateDiscoveryTasks(Guid eventId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var tasksCreated = await _mediator.Send(new GenerateDiscoveryTasksCommand
            {
                EventId = eventId,
                UserId = userId,
                SkipIfDiscoveryExists = true
            });

            return Ok(new
            {
                message = tasksCreated > 0
                    ? $"Generated {tasksCreated} discovery tasks."
                    : "Discovery tasks already exist for this event.",
                tasksCreated,
                taskPlanPhase = "Discovery"
            });
        }

        /// <summary>
        /// Realigns incomplete template tasks from today forward (fixes overdue schedules for mid-planning weddings).
        /// </summary>
        /// <response code="200">Schedule realigned.</response>
        /// <response code="401">Caller is not authenticated.</response>
        [HttpPost("api/events/{eventId:guid}/tasks/realign-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>Request DTO for assigning a task to an event team member.</summary>
    public record AssignTaskRequest(string? AssignedToUserId);

    /// <summary>Request DTO for updating task details.</summary>
    public record UpdateTaskRequest(
        string Title,
        string? Description,
        MyWedding.Domain.Enums.TaskStatus? Status,
        DateTime? StartDate,
        DateTime? DueDate,
        Guid? DependsOnTaskId,
        bool UpdateDependency = false);

    /// <summary>Request DTO for updating task schedule (Gantt).</summary>
    public record UpdateTaskScheduleRequest(
        DateTime? StartDate,
        DateTime? DueDate,
        Guid? DependsOnTaskId,
        bool UpdateDependency = false);

    /// <summary>Payload for applying a personalized checklist plan.</summary>
    /// <param name="ExcludeTemplateTitles">Template task titles to skip.</param>
    /// <param name="AdditionalTasks">Extra tasks to add beyond the template.</param>
    /// <param name="MarkBriefComplete">Whether to mark the event brief complete after apply.</param>
    public record ApplyChecklistPlanRequest(
        IReadOnlyList<string>? ExcludeTemplateTitles,
        IReadOnlyList<AdditionalChecklistTaskRequest>? AdditionalTasks,
        bool MarkBriefComplete = true);

    /// <summary>Additional task to include when applying a personalized checklist.</summary>
    public record AdditionalChecklistTaskRequest(
        string Title,
        string? Description,
        DateTime? StartDate,
        DateTime? DueDate);
}
