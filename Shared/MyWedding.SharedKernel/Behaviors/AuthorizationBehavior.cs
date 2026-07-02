using MediatR;
using MyWedding.SharedKernel.Exceptions;
using MyWedding.SharedKernel.Security;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyWedding.SharedKernel.Behaviors
{
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEventAuthorizationService _authService;

        public AuthorizationBehavior(IEventAuthorizationService authService)
        {
            _authService = authService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

            if (authorizeAttributes.Any())
            {
                // Simple implementation: if AuthorizeAttribute exists, assume it's an event-organizer check for now.
                // In a more complex system, we'd check roles/policies.
                
                if (request is IAuthorizeableRequest authorizeable)
                {
                    var isAuthorized = await _authService.IsOrganizerAsync(authorizeable.EventId, authorizeable.UserId);
                    
                    if (!isAuthorized)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }

            return await next();
        }
    }
}
