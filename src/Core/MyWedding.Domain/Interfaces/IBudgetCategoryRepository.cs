// File: src/Core/MyWedding.Domain/Interfaces/IBudgetCategoryRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IBudgetCategoryRepository
    {
        Task<IEnumerable<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    }
}
