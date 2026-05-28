// File: src/Core/MyWedding.Application/Features/Tasks/Queries/GetTasksByEventId/GetTasksByEventIdQueryHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
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
        private readonly IEventTaskRepository _taskRepository;

        public GetTasksByEventIdQueryHandler(
            IWeddingEventRepository eventRepository,
            IEventTaskRepository taskRepository)
        {
            _eventRepository = eventRepository;
            _taskRepository = taskRepository;
        }

        public async Task<IEnumerable<TaskDto>> Handle(GetTasksByEventIdQuery request, CancellationToken cancellationToken)
        {
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new ForbiddenAccessException("You do not have access to this event.");
            }

            var tasks = await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken);

            return tasks.Select(task => new TaskDto(
                task.Id,
                task.Title,
                task.Description,
                // Fully qualify enum type to avoid ambiguity with System.Threading.Tasks.TaskStatus
                ((MyWedding.Domain.Enums.TaskStatus)task.Status).ToString(),
                task.DueDate
            ));
        }
    }
}
