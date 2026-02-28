// File: src/Infrastructure/MyWedding.Infrastructure/Persistence/Repositories/VendorServiceRepository.cs
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
    public class VendorServiceRepository : IVendorServiceRepository
    {
        private readonly ApplicationDbContext _context;

        public VendorServiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VendorService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.VendorServices
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<VendorService>> GetByVendorIdAsync(string vendorId, CancellationToken cancellationToken = default)
        {
            return await _context.VendorServices
                .Include(s => s.Category)
                .Where(s => s.VendorId == vendorId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(VendorService service, CancellationToken cancellationToken = default)
        {
            await _context.VendorServices.AddAsync(service, cancellationToken);
        }

        public void Update(VendorService service)
        {
            _context.VendorServices.Update(service);
        }

        public void Delete(VendorService service)
        {
            _context.VendorServices.Remove(service);
        }
    }
}
