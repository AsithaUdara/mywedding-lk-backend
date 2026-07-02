using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.AssignTask;

public class AssignTaskCommandHandler : IRequestHandler<AssignTaskCommand>
{
    private readonly IEventTaskRepository _taskRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTaskCommandHandler(
        IEventTaskRepository taskRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        IUserRepository userRepository,
        ICollaborationService collaborationService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _collaborationService = collaborationService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            throw new NotFoundException($"Task with ID '{request.TaskId}' was not found.");

        var organizer = await _organizerRepository.GetOrganizerAsync(task.EventId, request.UserId, cancellationToken);
        var canManageViaOrganizer = organizer != null
            && organizer.PermissionLevel != PermissionLevel.Viewer;

        if (!canManageViaOrganizer)
        {
            var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(task.EventId, cancellationToken);
            if (weddingEvent is null)
                throw new NotFoundException($"Event for task '{request.TaskId}' was not found.");

            var canManageViaPlanner = weddingEvent.ManagingPlannerId == request.UserId
                || weddingEvent.CreatedById == request.UserId
                || await _eventRepository.IsManagedByPlannerAsync(
                    task.EventId,
                    request.UserId,
                    cancellationToken);

            if (!canManageViaPlanner)
                throw new ForbiddenAccessException("You do not have permission to assign tasks for this event.");
        }

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            var assigneeOrganizer = await _organizerRepository.GetOrganizerAsync(
                task.EventId,
                request.AssignedToUserId,
                cancellationToken);
            if (assigneeOrganizer is null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["AssignedToUserId"] = ["Assignee must be a member of this event team."]
                });
            }

            var user = await _userRepository.GetByIdAsync(request.AssignedToUserId, cancellationToken);
            if (user is null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["AssignedToUserId"] = ["Assigned user was not found."]
                });
            }
        }

        task.AssignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId)
            ? null
            : request.AssignedToUserId;
        task.UpdatedAt = DateTime.UtcNow;
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _collaborationService.NotifyChecklistUpdatedAsync(task.EventId);
    }
}
