using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Events.Application.Features.Invitations.Commands.InviteMember;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, InviteMemberResult>
{
    private readonly IEventInvitationRepository _invitationRepository;
    private readonly IEventOrganizerRepository _organizerRepository;
    private readonly IWeddingEventRepository _eventRepository;
    private readonly ICollaborationService _collaborationService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InviteMemberCommandHandler> _logger;

    public InviteMemberCommandHandler(
        IEventInvitationRepository invitationRepository,
        IEventOrganizerRepository organizerRepository,
        IWeddingEventRepository eventRepository,
        ICollaborationService collaborationService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<InviteMemberCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _organizerRepository = organizerRepository;
        _eventRepository = eventRepository;
        _collaborationService = collaborationService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InviteMemberResult> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.InvitedById, cancellationToken);
        if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
        {
            throw new MyWedding.SharedKernel.Exceptions.ForbiddenAccessException("You do not have permission to invite members to this event.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var frontendBase = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var acceptUrl = $"{frontendBase}/invite/accept?token={Uri.EscapeDataString(token)}";

        var invitation = new EventInvitation
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            Email = request.Email.Trim(),
            Token = token,
            InvitedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsAccepted = false,
            InvitedById = request.InvitedById,
            Role = request.Role,
            PermissionLevel = request.PermissionLevel
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        var emailSent = false;
        string? emailError = null;

        try
        {
            await _emailService.SendEventInvitationAsync(
                new EventInvitationEmailMessage(
                    request.Email,
                    weddingEvent?.EventName ?? "Your wedding",
                    request.EventId,
                    token,
                    request.Role.ToString(),
                    request.PermissionLevel.ToString()),
                cancellationToken);
            emailSent = true;
        }
        catch (Exception ex)
        {
            emailError = "Invitation was created but the email could not be delivered. Share the accept link manually.";
            _logger.LogError(
                ex,
                "Failed to send invitation email to {Email} for event {EventId}. Manual accept URL: {AcceptUrl}",
                request.Email,
                request.EventId,
                acceptUrl);
        }

        try
        {
            await _collaborationService.NotifyInvitationAcceptedAsync(request.EventId, request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invitation notification failed for event {EventId}", request.EventId);
        }

        return new InviteMemberResult(invitation.Id, emailSent, acceptUrl, emailError);
    }
}
