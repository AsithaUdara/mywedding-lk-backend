using MediatR;
using MyWedding.Domain.Interfaces;
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
                b.BookedBy != null ? $"{b.BookedBy.FirstName} {b.BookedBy.LastName}" : "Unknown",
                b.FinalAmount,
                b.Status,
                b.ServiceDate
            )).ToList();
        }
    }
}
