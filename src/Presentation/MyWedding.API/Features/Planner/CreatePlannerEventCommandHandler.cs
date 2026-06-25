using MediatR;
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Enums;
using MyWedding.Infrastructure.Persistence;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.API.Features.Planner.TaskTemplates;
using MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateDiscoveryTasks;
using MyWedding.Tasks.Application.Features.Tasks.Commands.GenerateTaskTemplate;

namespace MyWedding.API.Features.Planner;

public class CreatePlannerEventCommandHandler : IRequestHandler<CreatePlannerEventCommand, CreatePlannerEventResult>
{
    private readonly ApplicationDbContext _db;
    private readonly IMediator _mediator;

    public CreatePlannerEventCommandHandler(ApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<CreatePlannerEventResult> Handle(
        CreatePlannerEventCommand request,
        CancellationToken cancellationToken)
    {
        var planner = await _db.WeddingPlanners.FirstOrDefaultAsync(
            p => p.UserId == request.PlannerId,
            cancellationToken);
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

        await _db.WeddingEvents.AddAsync(weddingEvent, cancellationToken);
        await _db.EventOrganizers.AddRangeAsync(new[] { plannerOrganizer, clientOrganizer }, cancellationToken);
        await _db.PlannerClientEvents.AddAsync(plannerClientEvent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

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
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == clientUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(clientEmail))
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == clientEmail, cancellationToken);

        return null;
    }
}
