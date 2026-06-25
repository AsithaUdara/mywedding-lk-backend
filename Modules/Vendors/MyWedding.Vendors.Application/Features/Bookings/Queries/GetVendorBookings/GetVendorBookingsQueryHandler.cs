using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetVendorBookings
{
    public class GetVendorBookingsQueryHandler : IRequestHandler<GetVendorBookingsQuery, List<VendorBookingDto>>
    {
        private readonly IVendorBookingRepository _bookingRepository;

        public GetVendorBookingsQueryHandler(IVendorBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<List<VendorBookingDto>> Handle(GetVendorBookingsQuery request, CancellationToken cancellationToken)
        {
            var bookings = await _bookingRepository.GetBookingsByVendorUserIdAsync(request.VendorUserId, cancellationToken);
            
            return bookings.Select(b => new VendorBookingDto(
                b.Id,
                b.VendorService?.ServiceName ?? "Unknown",
                b.WeddingEvent?.EventName ?? "Unknown",
                UserDisplayNameHelper.GetDisplayName(
                    b.BookedBy?.FirstName,
                    b.BookedBy?.LastName,
                    b.BookedBy?.Email),
                b.BookedBy?.Email,
                b.FinalAmount,
                b.Status,
                b.ServiceDate,
                b.BookingContract?.ContractFileUrl,
                b.BookingContract?.VendorSignedAt,
                b.BookingContract?.ClientSignedAt,
                !string.IsNullOrWhiteSpace(b.BookingContract?.ContractFileUrl)
            )).ToList();
        }
    }
}
