using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class SavePlannerTaskTemplateFromEventCommandHandler
    : IRequestHandler<SavePlannerTaskTemplateFromEventCommand, SavePlannerTaskTemplateResult>
{
    private const int MaxTemplatesPerPlanner = 25;

    private readonly IPlannerTaskTemplateRepository _templateRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventTaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SavePlannerTaskTemplateFromEventCommandHandler(
        IPlannerTaskTemplateRepository templateRepository,
        IWeddingEventRepository eventRepository,
        IEventTaskRepository taskRepository,
        IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _eventRepository = eventRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SavePlannerTaskTemplateResult> Handle(
        SavePlannerTaskTemplateFromEventCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = ["Template name is required."]
            });
        }

        var templateCount = await _templateRepository.CountByPlannerIdAsync(
            request.PlannerId,
            cancellationToken);
        if (templateCount >= MaxTemplatesPerPlanner)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = [$"You can save up to {MaxTemplatesPerPlanner} custom templates."]
            });
        }

        var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        if (!await _eventRepository.IsManagedByPlannerAsync(
                request.EventId,
                request.PlannerId,
                cancellationToken))
        {
            throw new ForbiddenAccessException("Only the managing planner can save templates from this event.");
        }

        var tasks = (await _taskRepository.GetByEventIdAsync(request.EventId, cancellationToken)).ToList();
        if (tasks.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["eventId"] = ["Add tasks to this event before saving as a template."]
            });
        }

        var template = PlannerCustomTemplateMaterializer.BuildTemplateFromEventTasks(
            request.PlannerId,
            request.Name,
            request.Description,
            request.EventId,
            weddingEvent.EventDate,
            tasks);

        await _templateRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SavePlannerTaskTemplateResult(template.Id, template.Items.Count);
    }
}
