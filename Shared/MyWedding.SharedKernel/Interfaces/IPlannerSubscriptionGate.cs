namespace MyWedding.SharedKernel.Interfaces;

public interface IPlannerSubscriptionGate
{
    Task EnsureCanCreatePlannerEventAsync(string plannerId, CancellationToken cancellationToken = default);
}
