using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;

namespace MyWedding.Planner.Application.Features.Planner.TaskTemplates;

public class DeletePlannerTaskTemplateCommandHandler : IRequestHandler<DeletePlannerTaskTemplateCommand>
{
    private readonly IPlannerTaskTemplateRepository _templateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePlannerTaskTemplateCommandHandler(
        IPlannerTaskTemplateRepository templateRepository,
        IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePlannerTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(
            request.TemplateId,
            request.PlannerId,
            cancellationToken);

        if (template is null)
        {
            throw new NotFoundException("PlannerTaskTemplate", request.TemplateId);
        }

        _templateRepository.Remove(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
