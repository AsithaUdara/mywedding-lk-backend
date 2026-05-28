using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Interfaces;

public interface IBookingContractRepository
{
    Task<BookingContract?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task AddAsync(BookingContract contract, CancellationToken cancellationToken = default);
}
