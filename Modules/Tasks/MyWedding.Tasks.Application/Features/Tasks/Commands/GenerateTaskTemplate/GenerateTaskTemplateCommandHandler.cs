using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;

namespace MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

public class GenerateTaskTemplateCommandHandler : IRequestHandler<GenerateTaskTemplateCommand, int>
{
    private readonly IEventTaskRepository _taskRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateTaskTemplateCommandHandler(
        IEventTaskRepository taskRepository,
        IWeddingEventRepository eventRepository,
        ICollaborationService collaborationService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _eventRepository = eventRepository;
        _collaborationService = collaborationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GenerateTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var weddingEvent = await _eventRepository.GetByIdUnfilteredAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
            throw new NotFoundException("Event", request.EventId);

        var canManage = weddingEvent.ManagingPlannerId == request.UserId
            || weddingEvent.CreatedById == request.UserId
            || await _eventRepository.IsManagedByPlannerAsync(request.EventId, request.UserId, cancellationToken);

        if (!canManage)
            throw new ForbiddenAccessException("You do not have permission to generate tasks for this event.");

        if (weddingEvent.TaskPlanPhase == TaskPlanPhase.Full)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var created = await WeddingChecklistMaterializer.MaterializeFullChecklistAsync(
            request.EventId,
            weddingEvent.EventDate,
            _taskRepository,
            excludeTitles: null,
            additionalTasks: null,
            cancellationToken);

        weddingEvent.TaskPlanPhase = TaskPlanPhase.Full;
        if (weddingEvent.EventLifecycleStage is EventLifecycleStage.Lead or EventLifecycleStage.Onboarding)
        {
            weddingEvent.EventLifecycleStage = EventLifecycleStage.Planning;
        }

        weddingEvent.BriefCompletedAt ??= DateTime.UtcNow;
        weddingEvent.UpdatedAt = now;
        _eventRepository.Update(weddingEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);

        return created;
    }
}
