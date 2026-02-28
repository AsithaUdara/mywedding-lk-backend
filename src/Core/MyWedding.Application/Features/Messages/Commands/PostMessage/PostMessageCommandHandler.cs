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
        private readonly IConversationRepository _conversationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PostMessageCommandHandler(
            IMessageRepository messageRepository, 
            IConversationRepository conversationRepository,
            IEventOrganizerRepository organizerRepository,
            IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _organizerRepository = organizerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(PostMessageCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                throw new MyWedding.Application.Common.Exceptions.NotFoundException($"Conversation with ID '{request.ConversationId}' not found.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(conversation.EventId, request.SenderId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new MyWedding.Application.Common.Exceptions.ForbiddenAccessException("You do not have permission to post messages in this conversation.");
            }

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
