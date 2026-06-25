using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Tasks.Application.Templates;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class SavePlannerTaskTemplateFromEventCommandHandler
    : IRequestHandler<SavePlannerTaskTemplateFromEventCommand, SavePlannerTaskTemplateResult>
{
    private const int MaxTemplatesPerPlanner = 25;

    private readonly ApplicationDbContext _db;

    public SavePlannerTaskTemplateFromEventCommandHandler(ApplicationDbContext db)
    {
        _db = db;
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

        var templateCount = await _db.PlannerTaskTemplates
            .CountAsync(t => t.PlannerId == request.PlannerId, cancellationToken);
        if (templateCount >= MaxTemplatesPerPlanner)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = [$"You can save up to {MaxTemplatesPerPlanner} custom templates."]
            });
        }

        var weddingEvent = await _db.WeddingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
        if (weddingEvent is null)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        if (!await _db.WeddingEvents.AnyAsync(
                e => e.Id == request.EventId && e.ManagingPlannerId == request.PlannerId,
                cancellationToken))
        {
            throw new ForbiddenAccessException("Only the managing planner can save templates from this event.");
        }

        var tasks = await _db.EventTasks
            .AsNoTracking()
            .Where(t => t.EventId == request.EventId)
            .ToListAsync(cancellationToken);

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

        await _db.PlannerTaskTemplates.AddAsync(template, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new SavePlannerTaskTemplateResult(template.Id, template.Items.Count);
    }
}
