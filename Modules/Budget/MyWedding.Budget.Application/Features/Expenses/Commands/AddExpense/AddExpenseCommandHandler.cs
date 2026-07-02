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
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public AddExpenseCommandHandler(
            IExpenseRepository expenseRepository,
            IBudgetCategoryRepository categoryRepository,
            IWeddingEventRepository eventRepository,
            IEventOrganizerRepository organizerRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
            _organizerRepository = organizerRepository;
            _auditLogRepository = auditLogRepository;
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

            var auditItem = new AuditLogItem(
                Guid.NewGuid(),
                request.EventId,
                request.UserId,
                "ExpenseAdded",
                $"added a new expense: \"{request.Title}\" for {request.Amount:N2}",
                null,
                DateTime.UtcNow);
            await _auditLogRepository.AddAsync(auditItem, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fetch user for real-time activity update
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            // Notify real-time clients
            await _collaborationService.NotifyBudgetUpdatedAsync(request.EventId);
            await _collaborationService.NotifyActivityAsync(request.EventId, new
            {
                id = auditItem.Id,
                userId = auditItem.ActorId,
                userFirstName = user?.FirstName ?? "Team",
                userLastName = user?.LastName ?? "Member",
                itemType = auditItem.ActionType,
                content = auditItem.Content,
                createdAt = auditItem.TimestampUtc
            });

            return newExpense.Id;
        }
    }
}
