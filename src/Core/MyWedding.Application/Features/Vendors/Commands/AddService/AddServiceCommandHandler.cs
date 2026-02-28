using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Vendors.Commands.AddService
{
    public class AddServiceCommandHandler : IRequestHandler<AddServiceCommand, Guid>
    {
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddServiceCommandHandler(IVendorServiceRepository serviceRepository, IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(AddServiceCommand request, CancellationToken cancellationToken)
        {
            var service = new VendorService
            {
                Id = Guid.NewGuid(),
                VendorId = request.VendorId ?? string.Empty,
                ServiceName = request.ServiceName,
                ServiceDescription = request.Description,
                BasePrice = request.BasePrice,
                PricingType = request.PricingType,
                CategoryId = request.CategoryId,
                IsActive = request.IsActive
            };

            await _serviceRepository.AddAsync(service, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return service.Id;
        }
    }
}
