using MediatR;

namespace MyWedding.Events.Application.Features.Invitations.Commands.InviteMember;

public class InviteMemberCommand : IRequest<InviteMemberResult>
{
    public Guid EventId { get; set; }
    public required string Email { get; set; }
    public required string InvitedById { get; set; }
    public MyWedding.Domain.Enums.OrganizerRole Role { get; set; } = MyWedding.Domain.Enums.OrganizerRole.Friend;
    public MyWedding.Domain.Enums.PermissionLevel PermissionLevel { get; set; } = MyWedding.Domain.Enums.PermissionLevel.Editor;
}

public record InviteMemberResult(
    Guid InvitationId,
    bool EmailSent,
    string AcceptUrl,
    string? EmailError);
