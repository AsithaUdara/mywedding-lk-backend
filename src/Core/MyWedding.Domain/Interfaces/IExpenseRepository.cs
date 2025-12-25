// File: src/Core/MyWedding.Domain/Interfaces/IExpenseRepository.cs
using MyWedding.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Domain.Interfaces
{
    public interface IExpenseRepository
    {
        Task<IEnumerable<Expense>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    }
}
