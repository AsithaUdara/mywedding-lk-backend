// File: src/Core/MyWedding.Application/Features/Vendors/Commands/DeleteService/DeleteServiceCommandHandler.cs
using MediatR;


using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Vendors.Commands.DeleteService
{
    public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
    {
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteServiceCommandHandler(
            IVendorServiceRepository serviceRepository, 
            IVendorBookingRepository bookingRepository,
            IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            var service = await _serviceRepository.GetByIdAsync(request.Id, cancellationToken);
            if (service == null)
            {
                throw new NotFoundException($"Vendor service with ID '{request.Id}' not found.");
            }

            // Check if there are any bookings for this service
            var hasBookings = await _bookingRepository.HasBookingsAsync(request.Id, cancellationToken);
            if (hasBookings)
            {
                throw new BadRequestException("This service cannot be deleted because it has existing bookings. You can set it to 'Inactive' instead.");
            }

            _serviceRepository.Delete(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
