// File: src/Core/MyWedding.Application/Features/Tasks/Queries/GetTasksByEventId/GetTasksByEventIdQueryHandler.cs
using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Tasks.Application.Templates;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Tasks.Application.Features.Tasks.Queries.GetTasksByEventId
{
    public class GetTasksByEventIdQueryHandler : IRequestHandler<GetTasksByEventIdQuery, IEnumerable<TaskDto>>
    {
        private readonly IWeddingEventRepository _eventRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IEventTaskRepository _taskRepository;

        public GetTasksByEventIdQueryHandler(
            IWeddingEventRepository eventRepository,
            IEventOrganizerRepository organizerRepository,
            IEventTaskRepository taskRepository)
        {
            _eventRepository = eventRepository;
            _organizerRepository = organizerRepository;
            _taskRepository = taskRepository;
        }

        public async Task<IEnumerable<TaskDto>> Handle(GetTasksByEventIdQuery request, CancellationToken cancellationToken)
        {
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                if (string.IsNullOrEmpty(request.UserId))
                {
                    throw new ForbiddenAccessException("You do not have access to this event.");
                }

                var organizer = await _organizerRepository.GetOrganizerAsync(
                    request.EventId,
                    request.UserId,
                    cancellationToken);
                if (organizer is null)
                {
                    throw new ForbiddenAccessException("You do not have access to this event.");
                }

                weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
            }

            var tasks = (await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken)).ToList();

            if (weddingEvent is not null
                && weddingEvent.TaskPlanPhase != TaskPlanPhase.Full
                && !string.IsNullOrEmpty(request.UserId))
            {
                var canManage = weddingEvent.ManagingPlannerId == request.UserId
                    || weddingEvent.CreatedById == request.UserId
                    || await _eventRepository.IsManagedByPlannerAsync(
                        request.EventId,
                        request.UserId,
                        cancellationToken);

                if (!canManage)
                {
                    tasks = tasks
                        .Where(t => !ChecklistTemplatePlanner.IsDiscoveryTaskTitle(t.Title))
                        .ToList();
                }
            }

            return tasks.Select(task => new TaskDto(
                task.Id,
                task.Title,
                task.Description,
                ((MyWedding.Domain.Enums.TaskStatus)task.Status).ToString(),
                task.StartDate,
                task.DueDate,
                task.DependsOnTaskId,
                task.AssignedToUserId,
                task.CreatedAt
            ));
        }
    }
}
