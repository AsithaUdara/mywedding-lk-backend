// File: src/Core/MyWedding.Application/Features/EventOrganizers/Queries/GetOrganizersByEventId/OrganizerDto.cs
namespace MyWedding.Events.Application.Features.EventOrganizers.Queries.GetOrganizersByEventId
{
    public class OrganizerDto
    {
        public required string UserId { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Role { get; set; }
        public required string PermissionLevel { get; set; }
    }
}
