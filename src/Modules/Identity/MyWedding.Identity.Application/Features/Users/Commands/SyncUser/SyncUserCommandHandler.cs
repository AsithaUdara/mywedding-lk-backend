using MediatR;


using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Identity.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SyncUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> Handle(SyncUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Check if the user already exists in our database
            var existingUser = await _userRepository.GetByIdAsync(request.FirebaseUid, cancellationToken);

            // 2. If they already exist, do nothing. The sync is complete.
            if (existingUser != null)
            {
                return existingUser.Id;
            }

            // 3. If they don't exist, create a new User entity
            var newUser = new User
            {
                Id = request.FirebaseUid,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 4. Add the new user to the repository and save changes
            await _userRepository.AddAsync(newUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newUser.Id;
        }
    }
}
