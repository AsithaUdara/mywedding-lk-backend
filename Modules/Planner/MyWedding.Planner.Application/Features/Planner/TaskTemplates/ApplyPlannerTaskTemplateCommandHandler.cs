using MediatR;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class ApplyPlannerTaskTemplateCommandHandler
    : IRequestHandler<ApplyPlannerTaskTemplateCommand, ApplyPlannerTaskTemplateResult>
{
    private readonly IPlannerTaskTemplateRepository _templateRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventTaskRepository _taskRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyPlannerTaskTemplateCommandHandler(
        IPlannerTaskTemplateRepository templateRepository,
        IWeddingEventRepository eventRepository,
        IEventTaskRepository taskRepository,
        ICollaborationService collaborationService,
        IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _eventRepository = eventRepository;
        _taskRepository = taskRepository;
        _collaborationService = collaborationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyPlannerTaskTemplateResult> Handle(
        ApplyPlannerTaskTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdWithItemsAsync(
            request.TemplateId,
            request.PlannerId,
            cancellationToken);

        if (template is null)
        {
            throw new NotFoundException("PlannerTaskTemplate", request.TemplateId);
        }

        var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        if (weddingEvent.ManagingPlannerId != request.PlannerId)
        {
            throw new ForbiddenAccessException("Only the managing planner can apply templates to this event.");
        }

        int created;
        try
        {
            created = await PlannerCustomTemplateMaterializer.ApplyTemplateToEventAsync(
                template,
                request.EventId,
                weddingEvent.EventDate,
                _taskRepository,
                request.ReplaceExisting,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["eventId"] = [ex.Message]
            });
        }

        weddingEvent.TaskPlanPhase = created >= 20
            ? TaskPlanPhase.Full
            : TaskPlanPhase.Discovery;
        if (weddingEvent.EventLifecycleStage is EventLifecycleStage.Lead or EventLifecycleStage.Onboarding
            && weddingEvent.TaskPlanPhase == TaskPlanPhase.Full)
        {
            weddingEvent.EventLifecycleStage = EventLifecycleStage.Planning;
        }

        weddingEvent.UpdatedAt = DateTime.UtcNow;
        _eventRepository.Update(weddingEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);

        return new ApplyPlannerTaskTemplateResult(
            created,
            weddingEvent.TaskPlanPhase.ToString());
    }
}
