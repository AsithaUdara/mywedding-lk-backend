using MediatR;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Vendors.Application.Features.Bookings.Queries.GetEventBookings
{
    public class GetEventBookingsQueryHandler : IRequestHandler<GetEventBookingsQuery, List<EventBookingDto>>
    {
        private readonly IVendorBookingRepository _bookingRepository;

        public GetEventBookingsQueryHandler(IVendorBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<List<EventBookingDto>> Handle(GetEventBookingsQuery request, CancellationToken cancellationToken)
        {
            var bookings = await _bookingRepository.GetBookingsByEventIdAsync(request.EventId, cancellationToken);
            
            return bookings.Select(b => new EventBookingDto(
                b.Id,
                b.VendorService?.ServiceName ?? "Unknown",
                b.VendorService?.Vendor?.BusinessName ?? "Unknown",
                b.VendorService?.Vendor?.UserId,
                b.ServiceId,
                b.FinalAmount,
                b.Status,
                b.ServiceDate
            )).ToList();
        }
    }
}
