namespace MyWedding.Domain.ReadModels;

public class EventAuditStats
{
    public int TotalEntries { get; set; }
    public IReadOnlyDictionary<string, int> ActionTypeCounts { get; set; } =
        new Dictionary<string, int>();
}
