using MediatR;



using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyWedding.Vendors.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IVendorBookingRepository _bookingRepository;
        private readonly IExpenseRepository _expenseRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IBudgetCategoryRepository _categoryRepository;
        private readonly IWeddingEventRepository _eventRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IVendorBookingRepository bookingRepository,
            IExpenseRepository expenseRepository,
            IEventOrganizerRepository organizerRepository,
            IBudgetCategoryRepository categoryRepository,
            IWeddingEventRepository eventRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _expenseRepository = expenseRepository;
            _organizerRepository = organizerRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to create booking for EventId: {EventId} by UserId: {UserId}", request.EventId, request.UserId);

            // --- STRICT SECURITY CHECK: Only Owner can book ---
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Event with ID {request.EventId} not found.");
            }

            // Only the creator (Owner) is allowed to initiate a booking
            if (weddingEvent.CreatedById != request.UserId)
            {
                _logger.LogWarning("Forbidden: UserId {UserId} attempted to book for EventId {EventId} but is NOT the owner.", request.UserId, request.EventId);
                throw new ForbiddenAccessException("Only the Event Owner has the authority to book vendors.");
            }
            // --- END OF SECURITY CHECK ---

            var newBooking = new VendorBooking
            {
                Id = System.Guid.NewGuid(),
                EventId = request.EventId,
                ServiceId = request.ServiceId,
                FinalAmount = request.FinalAmount,
                ServiceDate = request.ServiceDate,
                BookedById = request.UserId,
                Status = Domain.Enums.BookingStatus.Confirmed,
                CreatedAt = System.DateTime.UtcNow
            };

            await _bookingRepository.AddAsync(newBooking, cancellationToken);

            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var otherCategory = categories.FirstOrDefault(c => c.Name.Equals("Other", System.StringComparison.OrdinalIgnoreCase));
            if (otherCategory is null)
            {
                throw new InvalidOperationException("Default 'Other' budget category not found.");
            }

            var newExpense = new Expense
            {
                Id = System.Guid.NewGuid(),
                EventId = request.EventId,
                Title = $"Booking for Service: {request.ServiceId}",
                Amount = request.FinalAmount,
                ExpenseDate = request.ServiceDate,
                BudgetCategoryId = otherCategory.Id,
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };
            await _expenseRepository.AddAsync(newExpense, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully created BookingId: {BookingId} for EventId: {EventId}", newBooking.Id, request.EventId);

            return newBooking.Id;
        }
    }
}
