using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Infrastructure.Persistence.Repositories;

public class BookingContractRepository : IBookingContractRepository
{
    private readonly ApplicationDbContext _context;

    public BookingContractRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<BookingContract?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return _context.BookingContracts.FirstOrDefaultAsync(c => c.Id == bookingId, cancellationToken);
    }

    public async Task AddAsync(BookingContract contract, CancellationToken cancellationToken = default)
    {
        await _context.BookingContracts.AddAsync(contract, cancellationToken);
    }
}
