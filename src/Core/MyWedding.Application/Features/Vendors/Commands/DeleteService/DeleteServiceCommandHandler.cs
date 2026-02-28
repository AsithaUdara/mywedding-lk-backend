// File: src/Core/MyWedding.Application/Features/Vendors/Commands/DeleteService/DeleteServiceCommandHandler.cs
using MediatR;
using MyWedding.Domain.Interfaces;
using MyWedding.Application.Common.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Vendors.Commands.DeleteService
{
    public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
    {
        private readonly IVendorServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteServiceCommandHandler(IVendorServiceRepository serviceRepository, IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            var service = await _serviceRepository.GetByIdAsync(request.Id, cancellationToken);
            if (service == null)
            {
                throw new NotFoundException($"Vendor service with ID '{request.Id}' not found.");
            }

            _serviceRepository.Delete(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
