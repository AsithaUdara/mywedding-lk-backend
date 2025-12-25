// File: src/Core/MyWedding.Application/Features/Tasks/Commands/UpdateTaskStatus/UpdateTaskStatusCommandHandler.cs
using MediatR;
using MyWedding.Application.Common.Exceptions;
using MyWedding.Domain.Interfaces;
using DomainTaskStatus = MyWedding.Domain.Enums.TaskStatus;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly IEventTaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTaskStatusCommandHandler(IEventTaskRepository taskRepository, IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
            {
                throw new NotFoundException($"Task with ID '{request.TaskId}' was not found.");
            }

            task.Status = request.NewStatus;
            task.UpdatedAt = System.DateTime.UtcNow;

            _taskRepository.Update(task); // Mark the entity as modified
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}