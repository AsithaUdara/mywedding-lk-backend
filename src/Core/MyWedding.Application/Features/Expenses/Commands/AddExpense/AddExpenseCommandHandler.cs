using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Expenses.Commands.AddExpense
{
    public class AddExpenseCommandHandler : IRequestHandler<AddExpenseCommand, Guid>
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly IBudgetCategoryRepository _categoryRepository; 
        private readonly IWeddingEventRepository _eventRepository;    
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddExpenseCommandHandler(
            IExpenseRepository expenseRepository,
            IBudgetCategoryRepository categoryRepository,
            IWeddingEventRepository eventRepository,
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            IUnitOfWork unitOfWork)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(AddExpenseCommand request, CancellationToken cancellationToken)
        {
            // --- VALIDATIONS ---
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding event with ID '{request.EventId}' not found.");
            }

            // --- SECURITY CHECK ---
            var organizer = await _organizerRepository.GetOrganizerAsync(request.EventId, request.UserId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new ForbiddenAccessException("You do not have permission to add expenses to this event.");
            }

            var categoryExists = await _categoryRepository.ExistsAsync(request.BudgetCategoryId, cancellationToken); 
            if (!categoryExists)
            {
                throw new NotFoundException($"Budget category with ID '{request.BudgetCategoryId}' not found.");
            }
            // --- END VALIDATIONS ---

            var newExpense = new Expense
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                Title = request.Title,
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                BudgetCategoryId = request.BudgetCategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _expenseRepository.AddAsync(newExpense, cancellationToken);

            // Log activity: User added an expense
            var activityItem = new ActivityFeedItem
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                UserId = request.UserId, // Assuming request contains UserId
                ItemType = MyWedding.Domain.Enums.ActivityType.SystemLog,
                Content = $"added a new expense: \"{request.Title}\" for {request.Amount:N2}",
                CreatedAt = DateTime.UtcNow
            };
            await _activityFeedRepository.AddAsync(activityItem, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newExpense.Id;
        }
    }
}
