// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/ExpenseRepository.cs
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpenseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Expense>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.Expenses
                .Where(e => e.EventId == eventId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            await _context.Expenses.AddAsync(expense, cancellationToken);
        }
    }
}
