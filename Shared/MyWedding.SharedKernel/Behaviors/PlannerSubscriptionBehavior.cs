using MediatR;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.SharedKernel.Behaviors;

/// <summary>
/// Enforces planner subscription tier limits before commands that create new client events.
/// </summary>
public class PlannerSubscriptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPlannerSubscriptionGate _subscriptionGate;

    public PlannerSubscriptionBehavior(IPlannerSubscriptionGate subscriptionGate)
    {
        _subscriptionGate = subscriptionGate;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IPlannerSubscriptionLimitedRequest limited &&
            !string.IsNullOrWhiteSpace(limited.PlannerId))
        {
            await _subscriptionGate.EnsureCanCreatePlannerEventAsync(limited.PlannerId, cancellationToken);
        }

        return await next();
    }
}
