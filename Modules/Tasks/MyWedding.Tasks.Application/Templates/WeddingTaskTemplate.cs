namespace MyWedding.Tasks.Application.Templates;

/// <summary>
/// Master wedding planning checklist: 50 tasks with offsets (days before wedding) and dependency indices.
/// </summary>
internal static class WeddingTaskTemplate
{
    internal readonly record struct Entry(
        string Title,
        int StartDaysBeforeWedding,
        int DueDaysBeforeWedding,
        int? DependsOnTemplateIndex);

    public static IReadOnlyList<Entry> Entries { get; } = new Entry[]
    {
        new("Define wedding vision and guest count", 365, 350, null),
        new("Set preliminary budget with planner", 350, 335, 0),
        new("Research and shortlist venues", 340, 310, 1),
        new("Book ceremony and reception venue", 310, 280, 2),
        new("Confirm venue capacity and layout", 280, 265, 3),
        new("Select wedding date and backup date", 300, 285, 3),
        new("Hire wedding planner coordinator (if needed)", 320, 300, 1),
        new("Create shared planning workspace", 335, 320, 1),
        new("Build vendor shortlist by category", 290, 270, 3),
        new("Send vendor proposals to client for approval", 275, 260, 8),
        new("Book photographer and videographer", 270, 240, 9),
        new("Book catering and tasting session", 265, 235, 9),
        new("Book DJ or live band", 260, 230, 9),
        new("Book florist and decor stylist", 255, 225, 9),
        new("Book officiant or religious coordinator", 250, 220, 9),
        new("Book hair and makeup artists", 245, 215, 9),
        new("Book transportation for wedding party", 240, 210, 9),
        new("Book accommodation blocks for guests", 235, 205, 9),
        new("Order wedding cake design and tasting", 230, 200, 12),
        new("Select wedding attire — bride", 220, 180, 3),
        new("Select wedding attire — groom", 220, 180, 3),
        new("Schedule attire fittings", 180, 150, 19),
        new("Design and order invitations", 200, 170, 6),
        new("Finalize guest list and addresses", 190, 175, 6),
        new("Mail save-the-dates", 210, 195, 23),
        new("Mail formal invitations", 150, 120, 22),
        new("Track RSVPs and meal preferences", 120, 30, 25),
        new("Plan ceremony program and readings", 120, 60, 14),
        new("Plan reception timeline and speeches", 110, 45, 27),
        new("Choose wedding party", 200, 185, 6),
        new("Arrange bachelor and bachelorette events", 90, 60, 29),
        new("Book rehearsal dinner venue", 100, 70, 3),
        new("Plan honeymoon travel", 150, 90, 1),
        new("Apply for marriage license", 60, 45, 6),
        new("Confirm all vendor contracts signed", 90, 75, 10),
        new("Create seating chart draft", 75, 40, 26),
        new("Finalize seating chart", 35, 14, 35),
        new("Confirm final headcount with caterer", 21, 10, 26),
        new("Create day-of emergency kit", 30, 7, 6),
        new("Prepare vendor payment schedule", 120, 90, 2),
        new("Pay vendor deposits per contract", 100, 80, 39),
        new("Confirm delivery times with all vendors", 14, 7, 34),
        new("Walkthrough venue with planner", 21, 14, 4),
        new("Finalize playlist and do-not-play list", 30, 14, 13),
        new("Confirm rehearsal schedule with wedding party", 10, 5, 31),
        new("Pack for honeymoon", 7, 2, 32),
        new("Prepare wedding day timeline document", 14, 3, 28),
        new("Distribute day-of contact sheet", 7, 2, 46),
        new("Final attire pickup and steaming", 5, 1, 21),
        new("Rehearsal and rehearsal dinner", 2, 1, 44),
        new("Wedding day execution", 0, 0, 49)
    };
}
