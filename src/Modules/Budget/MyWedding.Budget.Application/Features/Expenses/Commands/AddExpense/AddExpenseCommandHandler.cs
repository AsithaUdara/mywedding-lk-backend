using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;

namespace MyWedding.Budget.Application.Features.Expenses.Commands.AddExpense
{
    public class AddExpenseCommandHandler : IRequestHandler<AddExpenseCommand, Guid>
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly IBudgetCategoryRepository _categoryRepository; 
        private readonly IWeddingEventRepository _eventRepository;    
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IActivityFeedRepository _activityFeedRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public AddExpenseCommandHandler(
            IExpenseRepository expenseRepository,
            IBudgetCategoryRepository categoryRepository,
            IWeddingEventRepository eventRepository,
            IEventOrganizerRepository organizerRepository,
            IActivityFeedRepository activityFeedRepository,
            IUserRepository userRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
            _organizerRepository = organizerRepository;
            _activityFeedRepository = activityFeedRepository;
            _userRepository = userRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(AddExpenseCommand request, CancellationToken cancellationToken)
        {
            // --- VALIDATIONS ---
            var categoryExists = await _categoryRepository.ExistsAsync(request.BudgetCategoryId, cancellationToken); 
            if (!categoryExists)
            {
                throw new NotFoundException(nameof(BudgetCategory), request.BudgetCategoryId);
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

            // Fetch user for real-time activity update
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyBudgetUpdatedAsync(request.EventId);
            await _collaborationService.NotifyActivityAsync(request.EventId, new
            {
                id = activityItem.Id,
                userId = activityItem.UserId,
                userFirstName = user?.FirstName ?? "Team",
                userLastName = user?.LastName ?? "Member",
                itemType = activityItem.ItemType.ToString(),
                content = activityItem.Content,
                createdAt = activityItem.CreatedAt
            });

            return newExpense.Id;
        }
    }
}
