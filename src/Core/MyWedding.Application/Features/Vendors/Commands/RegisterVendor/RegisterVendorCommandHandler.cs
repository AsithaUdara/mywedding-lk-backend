// File: src/Core/MyWedding.Application/Features/Vendors/Commands/RegisterVendor/RegisterVendorCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Vendors.Commands.RegisterVendor
{
    public class RegisterVendorCommandHandler : IRequestHandler<RegisterVendorCommand, string>
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterVendorCommandHandler(
            IVendorRepository vendorRepository,
            IUserRepository userRepository,
            IFirebaseAuthService firebaseAuthService,
            IUnitOfWork unitOfWork)
        {
            _vendorRepository = vendorRepository;
            _userRepository = userRepository;
            _firebaseAuthService = firebaseAuthService;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> Handle(RegisterVendorCommand request, CancellationToken cancellationToken)
        {
            // 1. Ensure the user exists in our local DB
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                user = new User
                {
                    Id = request.UserId,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _userRepository.AddAsync(user, cancellationToken);
            }

            // 2. Map Category string to GUID (Simplified for now based on seeded IDs)
            Guid categoryId = request.Category.ToLower() switch
            {
                "venue" => Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "photography" => Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "catering" => Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "floral" => Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "music" => Guid.Parse("55555555-5555-5555-5555-555555555555"),
                _ => Guid.Parse("66666666-6666-6666-6666-666666666666")
            };

            // 3. Check if Vendor already exists
            var existingVendor = await _vendorRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (existingVendor != null)
            {
                return existingVendor.UserId;
            }

            // 4. Create the Vendor record
            var vendor = new Vendor
            {
                UserId = request.UserId,
                BusinessName = request.BusinessName,
                City = request.City,
                PrimaryCategoryId = categoryId,
                VerificationStatus = VerificationStatus.Pending,
                AverageRating = 0
            };

            await _vendorRepository.AddAsync(vendor, cancellationToken);
            
            // 5. Set Firebase Custom Claim
            try 
            {
                await _firebaseAuthService.SetUserRoleAsync(request.UserId, "vendor");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to set Firebase role for user {request.UserId}: {ex.Message}");
            }

            try 
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database error during vendor registration: {ex.Message}. Make sure database is seeded.", ex);
            }

            return vendor.UserId;
        }
    }
}
