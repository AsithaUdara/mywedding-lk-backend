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

            // 2. If they already exist, refresh placeholder profile details when possible.
            if (existingUser != null)
            {
                var updated = false;

                if (!string.IsNullOrWhiteSpace(request.Email) &&
                    !string.Equals(existingUser.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser.Email = request.Email;
                    updated = true;
                }

                var hasIncomingName = !string.IsNullOrWhiteSpace(request.FirstName) &&
                    !(string.Equals(request.FirstName, "User", StringComparison.OrdinalIgnoreCase) &&
                      string.IsNullOrWhiteSpace(request.LastName));
                var hasPlaceholderName = string.IsNullOrWhiteSpace(existingUser.FirstName) ||
                    (string.Equals(existingUser.FirstName, "User", StringComparison.OrdinalIgnoreCase) &&
                     string.IsNullOrWhiteSpace(existingUser.LastName));

                if (hasIncomingName &&
                    (hasPlaceholderName ||
                     !string.Equals(existingUser.FirstName, request.FirstName, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(existingUser.LastName ?? string.Empty, request.LastName ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
                {
                    existingUser.FirstName = request.FirstName;
                    existingUser.LastName = request.LastName ?? string.Empty;
                    updated = true;
                }

                if (updated)
                {
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

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
