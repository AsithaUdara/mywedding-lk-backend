using MediatR;

namespace MyWedding.Identity.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserCommand : IRequest<string>
    {
        public required string FirebaseUid { get; init; }
        public required string Email { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
    }
}
