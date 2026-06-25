using MediatR;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Vendors.Application.Features.Availability.Queries.GetVendorAvailability
{
    public record BlockedDateDetailDto(string Date, string? Reason);

    public record VendorAvailabilityDto(
        IReadOnlyList<int> BookedDates,
        IReadOnlyList<int> BlockedDates,
        IReadOnlyList<BlockedDateDetailDto> BlockedDateDetails);

    public class GetVendorAvailabilityQuery : IRequest<VendorAvailabilityDto?>
    {
        public required string VendorId { get; init; }
        public required int Year { get; init; }
        public required int Month { get; init; }
    }

    public class GetVendorAvailabilityQueryHandler : IRequestHandler<GetVendorAvailabilityQuery, VendorAvailabilityDto?>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IVendorBlockedDateRepository _blockedDateRepository;

        public GetVendorAvailabilityQueryHandler(
            IVendorBookingRepository bookingRepository,
            IVendorBlockedDateRepository blockedDateRepository)
        {
            _bookingRepository = bookingRepository;
            _blockedDateRepository = blockedDateRepository;
        }

        public async Task<VendorAvailabilityDto?> Handle(
            GetVendorAvailabilityQuery request,
            CancellationToken cancellationToken)
        {
            if (request.Month is < 1 or > 12)
            {
                return null;
            }

            var bookedDates = await _bookingRepository.GetBookedDaysForVendorInMonthAsync(
                request.VendorId,
                request.Year,
                request.Month,
                cancellationToken);

            var blocked = await _blockedDateRepository.GetByVendorAndMonthAsync(
                request.VendorId,
                request.Year,
                request.Month,
                cancellationToken);

            return new VendorAvailabilityDto(
                bookedDates,
                blocked.Select(b => b.Date.Day).Distinct().OrderBy(d => d).ToList(),
                blocked.Select(b => new BlockedDateDetailDto(
                    b.Date.ToString("yyyy-MM-dd"),
                    b.Reason)).ToList());
        }
    }
}
