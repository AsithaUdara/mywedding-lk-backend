// File: src/Core/MyWedding.Application/Features/Expenses/Commands/AddExpense/AddExpenseCommandHandler.cs
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
        private readonly IBudgetCategoryRepository _categoryRepository; // Need this to validate category
        private readonly IWeddingEventRepository _eventRepository;    // Need this to validate event exists
        private readonly IUnitOfWork _unitOfWork;

        public AddExpenseCommandHandler(
            IExpenseRepository expenseRepository,
            IBudgetCategoryRepository categoryRepository,
            IWeddingEventRepository eventRepository,
            IUnitOfWork unitOfWork)
        {
            _expenseRepository = expenseRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(AddExpenseCommand request, CancellationToken cancellationToken)
        {
            // --- VALIDATIONS ---
            // Check if the event exists
            var weddingEvent = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (weddingEvent is null)
            {
                throw new NotFoundException($"Wedding event with ID '{request.EventId}' not found.");
            }

            // Check if the budget category is valid
            var categoryExists = await _categoryRepository.ExistsAsync(request.BudgetCategoryId, cancellationToken); // Assuming this method exists or will be added
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newExpense.Id;
        }
    }
}
