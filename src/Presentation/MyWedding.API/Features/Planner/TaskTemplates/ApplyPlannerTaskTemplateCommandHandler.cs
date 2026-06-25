using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class ApplyPlannerTaskTemplateCommandHandler
    : IRequestHandler<ApplyPlannerTaskTemplateCommand, ApplyPlannerTaskTemplateResult>
{
    private readonly ApplicationDbContext _db;
    private readonly IEventTaskRepository _taskRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyPlannerTaskTemplateCommandHandler(
        ApplicationDbContext db,
        IEventTaskRepository taskRepository,
        ICollaborationService collaborationService,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _taskRepository = taskRepository;
        _collaborationService = collaborationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyPlannerTaskTemplateResult> Handle(
        ApplyPlannerTaskTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _db.PlannerTaskTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.PlannerId == request.PlannerId,
                cancellationToken);

        if (template is null)
        {
            throw new NotFoundException("PlannerTaskTemplate", request.TemplateId);
        }

        var weddingEvent = await _db.WeddingEvents
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
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
        _db.WeddingEvents.Update(weddingEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _collaborationService.NotifyChecklistUpdatedAsync(request.EventId);

        return new ApplyPlannerTaskTemplateResult(
            created,
            weddingEvent.TaskPlanPhase.ToString());
    }
}
