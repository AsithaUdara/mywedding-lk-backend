using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Domain.Interfaces;
using MyWedding.Planner.Application.Features.Planner;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Planner.Application.Features.Planner.TaskTemplates;
using MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateDiscoveryTasks;
using MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

namespace MyWedding.Planner.Application.Features.Planner;

public class CreatePlannerEventCommandHandler : IRequestHandler<CreatePlannerEventCommand, CreatePlannerEventResult>
{
    private readonly IWeddingPlannerRepository _plannerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IPlannerClientEventRepository _plannerClientEventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public CreatePlannerEventCommandHandler(
        IWeddingPlannerRepository plannerRepository,
        IUserRepository userRepository,
        IWeddingEventRepository eventRepository,
        IEventOrganizerRepository organizerRepository,
        IPlannerClientEventRepository plannerClientEventRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _plannerRepository = plannerRepository;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _organizerRepository = organizerRepository;
        _plannerClientEventRepository = plannerClientEventRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<CreatePlannerEventResult> Handle(
        CreatePlannerEventCommand request,
        CancellationToken cancellationToken)
    {
        var planner = await _plannerRepository.GetByUserIdAsync(request.PlannerId, cancellationToken);
        if (planner is null)
            throw new ForbiddenAccessException();

        var clientUser = await ResolveClientUserAsync(
            request.ClientUserId,
            request.ClientEmail,
            cancellationToken);
        if (clientUser is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["client"] = ["Client user not found. Provide a valid clientUserId or synced client email."]
            });
        }

        var weddingEvent = new WeddingEvent
        {
            Id = Guid.NewGuid(),
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate,
            TotalBudget = request.TotalBudget,
            ManagingPlannerId = request.PlannerId,
            EventLifecycleStage = EventLifecycleStage.Lead,
            CreatedById = request.PlannerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var plannerOrganizer = new EventOrganizer
        {
            EventId = weddingEvent.Id,
            UserId = request.PlannerId,
            Role = OrganizerRole.Planner,
            PermissionLevel = PermissionLevel.Owner,
            JoinedAt = DateTime.UtcNow
        };

        var clientOrganizer = new EventOrganizer
        {
            EventId = weddingEvent.Id,
            UserId = clientUser.Id,
            Role = OrganizerRole.Bride,
            PermissionLevel = PermissionLevel.Editor,
            JoinedAt = DateTime.UtcNow
        };

        var plannerClientEvent = new PlannerClientEvent
        {
            Id = Guid.NewGuid(),
            PlannerId = request.PlannerId,
            EventId = weddingEvent.Id,
            ClientUserId = clientUser.Id,
            Status = PlannerClientEventStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(weddingEvent, cancellationToken);
        await _organizerRepository.AddAsync(plannerOrganizer, cancellationToken);
        await _organizerRepository.AddAsync(clientOrganizer, cancellationToken);
        await _plannerClientEventRepository.AddAsync(plannerClientEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tasksGenerated = request.TaskSeedMode switch
        {
            EventTaskSeedMode.Manual => 0,
            EventTaskSeedMode.DiscoveryStarter => await _mediator.Send(
                new GenerateDiscoveryTasksCommand
                {
                    EventId = weddingEvent.Id,
                    UserId = request.PlannerId,
                    SkipIfDiscoveryExists = false
                },
                cancellationToken),
            EventTaskSeedMode.MasterChecklist => await _mediator.Send(
                new GenerateTaskTemplateCommand
                {
                    EventId = weddingEvent.Id,
                    UserId = request.PlannerId,
                    SkipIfTasksExist = false
                },
                cancellationToken),
            EventTaskSeedMode.CustomTemplate when request.CustomTemplateId.HasValue => (
                await _mediator.Send(
                    new ApplyPlannerTaskTemplateCommand
                    {
                        PlannerId = request.PlannerId,
                        EventId = weddingEvent.Id,
                        TemplateId = request.CustomTemplateId.Value,
                        ReplaceExisting = true
                    },
                    cancellationToken)).TasksCreated,
            EventTaskSeedMode.CustomTemplate => throw new ValidationException(new Dictionary<string, string[]>
            {
                ["customTemplateId"] = ["Select a custom template to use this option."]
            }),
            _ => 0
        };

        return new CreatePlannerEventResult(
            weddingEvent.Id,
            plannerClientEvent.Id,
            tasksGenerated);
    }

    private async Task<User?> ResolveClientUserAsync(
        string? clientUserId,
        string? clientEmail,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(clientUserId))
            return await _userRepository.GetByIdAsync(clientUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(clientEmail))
            return await _userRepository.GetByEmailAsync(clientEmail, cancellationToken);

        return null;
    }
}
