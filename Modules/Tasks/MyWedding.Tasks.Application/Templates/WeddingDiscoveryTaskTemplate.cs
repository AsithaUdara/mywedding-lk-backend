namespace MyWedding.Tasks.Application.Templates;

/// <summary>
/// Lead-phase discovery checklist (requirements before full Gantt generation).
/// Offsets are days from plan start (today), not days before wedding.
/// </summary>
internal static class WeddingDiscoveryTaskTemplate
{
    internal readonly record struct Entry(
        string Title,
        int StartOffsetDays,
        int DueOffsetDays,
        int? DependsOnTemplateIndex);

    public static IReadOnlyList<Entry> Entries { get; } = new Entry[]
    {
        new("Complete couple discovery questionnaire", 0, 7, null),
        new("Schedule kickoff consultation call", 2, 10, 0),
        new("Confirm guest count range and budget band", 5, 14, 1),
        new("Capture wedding vision, culture, and must-haves", 7, 18, 2),
        new("Document venue preferences and date constraints", 10, 21, 3),
        new("Draft planner proposal and service scope", 14, 28, 4),
        new("Send proposal for client review and sign-off", 21, 35, 5),
        new("Client engaged — generate master planning checklist", 28, 42, 6),
    };
}
