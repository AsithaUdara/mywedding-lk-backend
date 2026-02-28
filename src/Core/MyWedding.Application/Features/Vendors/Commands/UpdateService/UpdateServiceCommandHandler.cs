// File: src/Core/MyWedding.Application/Features/Vendors/Commands/UpdateService/UpdateServiceCommandHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Vendors.Commands.UpdateService
{
    public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, bool>
    {
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateServiceCommandHandler(IVendorServiceRepository serviceRepository, IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            var service = await _serviceRepository.GetByIdAsync(request.Id, cancellationToken);
            if (service == null)
            {
                throw new NotFoundException($"Vendor service with ID '{request.Id}' not found.");
            }

            service.ServiceName = request.ServiceName;
            service.ServiceDescription = request.Description;
            service.BasePrice = request.BasePrice;
            service.PricingType = request.PricingType;
            service.CategoryId = request.CategoryId;
            service.IsActive = request.IsActive;

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
