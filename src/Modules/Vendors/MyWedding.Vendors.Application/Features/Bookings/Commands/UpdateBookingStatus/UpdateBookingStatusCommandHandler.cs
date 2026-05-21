using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.UpdateBookingStatus
{
    public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, bool>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBookingStatusCommandHandler(IVendorBookingRepository bookingRepository, IUnitOfWork unitOfWork)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

            if (booking == null)
            {
                throw new NotFoundException("Booking", request.BookingId);
            }

            if (booking.VendorService?.Vendor?.UserId != request.VendorUserId)
            {
                throw new ForbiddenAccessException();
            }

            booking.Status = request.NewStatus;
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
