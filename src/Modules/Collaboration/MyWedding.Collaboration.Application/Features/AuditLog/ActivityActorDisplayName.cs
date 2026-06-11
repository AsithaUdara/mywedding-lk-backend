using MyWedding.Domain.Enums;
using MyWedding.SharedKernel;

namespace MyWedding.Collaboration.Application.Features.AuditLog;

internal static class ActivityActorDisplayName
{
    public static string Resolve(
        string? firstName,
        string? lastName,
        string? email,
        string actorId,
        string? managingPlannerId,
        OrganizerRole? organizerRole)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        var full = $"{first} {last}".Trim();
        var isPlaceholder = string.IsNullOrEmpty(full)
            || (string.Equals(first, "User", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(last));

        if (!isPlaceholder)
            return full;

        if (!string.IsNullOrEmpty(managingPlannerId)
            && string.Equals(actorId, managingPlannerId, StringComparison.Ordinal))
        {
            return "Planner";
        }

        if (organizerRole == OrganizerRole.Planner)
            return "Planner";

        if (organizerRole is OrganizerRole.Bride
            or OrganizerRole.Groom
            or OrganizerRole.Owner)
        {
            return "Client";
        }

        return UserDisplayNameHelper.GetDisplayName(firstName, lastName, email);
    }
}
