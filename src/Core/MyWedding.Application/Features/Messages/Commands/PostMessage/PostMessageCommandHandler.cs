// File: .../PostMessage/PostMessageCommandHandler.cs
using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Application.Features.Messages.Commands.PostMessage
{
    public class PostMessageCommandHandler : IRequestHandler<PostMessageCommand, Guid>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        // TODO: Inject other repositories to validate attachment IDs

        public PostMessageCommandHandler(IMessageRepository messageRepository, IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(PostMessageCommand request, CancellationToken cancellationToken)
        {
            // TODO: Add security check to ensure user is a member of this conversation's event

            var newMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                
                // Assign attachments if they exist
                AttachedVendorServiceId = request.AttachedVendorServiceId,
                AttachedEventTaskId = request.AttachedEventTaskId,
                AttachedExpenseId = request.AttachedExpenseId
            };

            await _messageRepository.AddAsync(newMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newMessage.Id;
        }
    }
}
