using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.API.Features.Planner.TaskTemplates;

public class DeletePlannerTaskTemplateCommandHandler : IRequestHandler<DeletePlannerTaskTemplateCommand>
{
    private readonly ApplicationDbContext _db;

    public DeletePlannerTaskTemplateCommandHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeletePlannerTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _db.PlannerTaskTemplates
            .FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.PlannerId == request.PlannerId,
                cancellationToken);

        if (template is null)
        {
            throw new NotFoundException("PlannerTaskTemplate", request.TemplateId);
        }

        _db.PlannerTaskTemplates.Remove(template);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
