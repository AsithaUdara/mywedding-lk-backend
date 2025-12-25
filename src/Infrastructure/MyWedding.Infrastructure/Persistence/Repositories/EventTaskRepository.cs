// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/EventTaskRepository.cs

using Microsoft.EntityFrameworkCore;
using MyWedding.Infrastructure.Persistence;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Persistence.Repositories
{
    public class EventTaskRepository : IEventTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public EventTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EventTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _context.EventTasks.FindAsync(new object[] { taskId }, cancellationToken);
        }

        public async Task<IEnumerable<EventTask>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _context.EventTasks
                .Where(t => t.EventId == eventId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(EventTask task, CancellationToken cancellationToken = default)
        {
            await _context.EventTasks.AddAsync(task, cancellationToken);
        }

        public void Update(EventTask task)
        {
            // EF Core's change tracker automatically detects modifications to an entity
            // that is being tracked. By calling Update, we are just being explicit
            // that this entity has been modified.
            _context.EventTasks.Update(task);
        }

        public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var taskToDelete = await _context.EventTasks.FindAsync(new object[] { taskId }, cancellationToken);
            if (taskToDelete != null)
            {
                _context.EventTasks.Remove(taskToDelete);
            }
        }
    }
}