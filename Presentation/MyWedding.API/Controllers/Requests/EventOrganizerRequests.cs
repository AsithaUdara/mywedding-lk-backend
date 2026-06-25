using MyWedding.Domain.Enums;

namespace MyWedding.API.Controllers.Requests;

/// <summary>Request DTO for inviting a user to a wedding event as an organizer.</summary>
public record InviteUserRequest(string Email, OrganizerRole Role, PermissionLevel PermissionLevel);

/// <summary>Request DTO for updating an existing organizer's role and permission level.</summary>
public record UpdateOrganizerRequest(OrganizerRole Role, PermissionLevel PermissionLevel);
