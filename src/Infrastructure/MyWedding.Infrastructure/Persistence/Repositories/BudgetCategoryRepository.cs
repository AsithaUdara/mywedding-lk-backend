// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/BudgetCategoryRepository.cs
using Microsoft.EntityFrameworkCore;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class BudgetCategoryRepository : IBudgetCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public BudgetCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.BudgetCategories.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BudgetCategories.AnyAsync(c => c.Id == id, cancellationToken);
        }
    }
}
