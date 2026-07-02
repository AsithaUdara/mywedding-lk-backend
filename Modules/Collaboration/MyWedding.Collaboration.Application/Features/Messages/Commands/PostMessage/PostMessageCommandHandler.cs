using MediatR;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Interfaces;
using MyWedding.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.Collaboration.Application.Features.Messages.Commands.PostMessage
{
    public class PostMessageCommandHandler : IRequestHandler<PostMessageCommand, Guid>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IEventOrganizerRepository _organizerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICollaborationService _collaborationService;
        private readonly IUnitOfWork _unitOfWork;

        public PostMessageCommandHandler(
            IMessageRepository messageRepository, 
            IConversationRepository conversationRepository,
            IEventOrganizerRepository organizerRepository,
            IUserRepository userRepository,
            ICollaborationService collaborationService,
            IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _organizerRepository = organizerRepository;
            _userRepository = userRepository;
            _collaborationService = collaborationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(PostMessageCommand request, CancellationToken cancellationToken)
        {
            // --- SECURITY CHECK ---
            var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                throw new MyWedding.SharedKernel.Exceptions.NotFoundException($"Conversation with ID '{request.ConversationId}' not found.");
            }

            var organizer = await _organizerRepository.GetOrganizerAsync(conversation.EventId, request.SenderId, cancellationToken);
            if (organizer == null || organizer.PermissionLevel == MyWedding.Domain.Enums.PermissionLevel.Viewer)
            {
                throw new MyWedding.SharedKernel.Exceptions.ForbiddenAccessException("You do not have permission to post messages in this conversation.");
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

            // Fetch sender info for real-time DTO
            var sender = await _userRepository.GetByIdAsync(request.SenderId, cancellationToken);

            // Broadcast real-time signal
            await _collaborationService.NotifyMessageAsync(conversation.EventId, new
            {
                id = newMessage.Id,
                conversationId = newMessage.ConversationId,
                content = newMessage.Content,
                createdAt = newMessage.CreatedAt,
                senderId = newMessage.SenderId,
                senderFirstName = sender?.FirstName ?? string.Empty,
                senderLastName = sender?.LastName ?? string.Empty,
                senderEmail = sender?.Email ?? string.Empty,
                attachment = (object?)null // Simplified for now
            });

            return newMessage.Id;
        }
    }
}
