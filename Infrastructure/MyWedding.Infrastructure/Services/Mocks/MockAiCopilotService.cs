using System.Text;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services.Mocks;

/// <summary>
/// Simulates OpenAI GPT-4o co-pilot responses until a real provider is wired.
/// </summary>
// TODO: Human to implement — call OpenAI API using configuration key OpenAI:ApiKey.
public class MockAiCopilotService : IAiCopilotService
{
    public async Task<InquiryEmailDraftResult> DraftInquiryEmailAsync(
        InquiryEmailDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(120, cancellationToken);

        var budgetLine = request.BudgetLkr.HasValue
            ? $"Our allocated budget for this category is approximately LKR {request.BudgetLkr:N0}."
            : "We are flexible on budget for the right package and deliverables.";

        var venueLine = string.IsNullOrWhiteSpace(request.Venue)
            ? "Venue is still being finalized."
            : $"The event will be held at {request.Venue}.";

        var requirements = request.ServiceRequirements is { Count: > 0 }
            ? string.Join("; ", request.ServiceRequirements)
            : "full-day coverage with online gallery delivery";

        var styleLine = string.IsNullOrWhiteSpace(request.StyleNotes)
            ? "The couple prefers a warm, candid documentary style."
            : request.StyleNotes;

        var subject =
            $"Quote request — {request.EventName} ({request.WeddingDate:dd MMM yyyy})";

        var body = new StringBuilder()
            .AppendLine($"Dear {request.VendorBusinessName} team,")
            .AppendLine()
            .AppendLine(
                $"I am {request.PlannerName}, planning \"{request.EventName}\" on {request.WeddingDate:dddd, dd MMMM yyyy}. " +
                $"We are sourcing a trusted {request.VendorCategory} partner for this celebration.")
            .AppendLine()
            .AppendLine(venueLine)
            .AppendLine(budgetLine)
            .AppendLine()
            .AppendLine($"Key requirements: {requirements}.")
            .AppendLine($"Creative direction: {styleLine}")
            .AppendLine()
            .AppendLine("Could you please share:")
            .AppendLine("• Your recommended package and all-in pricing")
            .AppendLine("• Peak-season availability for our date")
            .AppendLine("• Deposit terms and delivery timeline")
            .AppendLine()
            .AppendLine("Happy to jump on a short call this week if helpful.")
            .AppendLine()
            .AppendLine($"Warm regards,\n{request.PlannerName}\nvia MyWedding.lk")
            .ToString();

        return new InquiryEmailDraftResult(subject, body, IsSimulated: true);
    }

    public async Task<MeetingSummaryResult> SummarizeMeetingToTasksAsync(
        MeetingSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        var summary =
            $"Discovery call for \"{request.EventName}\": couple confirmed ceremony timing, discussed vendor shortlist, " +
            "and agreed on budget guardrails. Follow-ups assigned to planner for quotes and venue walkthrough.";

        var tasks = new List<ProposedTaskItem>
        {
            new(
                "Send photography shortlist to couple",
                "Share 3 verified vendors with sample galleries aligned to documentary style.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                "High"),
            new(
                "Book venue walkthrough",
                "Coordinate with venue coordinator for lighting check before 4 PM ceremony.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                "Medium"),
            new(
                "Update budget tracker — catering deposit",
                "Log LKR 150,000 deposit discussed on call; flag 8% over category average.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                "High"),
            new(
                "Draft day-of timeline v1",
                "Block ceremony 4:00 PM, sunset portraits 6:15 PM, reception entrance 7:00 PM.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                "Medium"),
        };

        return new MeetingSummaryResult(summary, tasks, IsSimulated: true);
    }

    public async Task<PersonalizedChecklistPlanResult> GeneratePersonalizedChecklistPlanAsync(
        PersonalizedChecklistPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(180, cancellationToken);

        var guestLine = request.EstimatedGuestCount is > 0
            ? $"~{request.EstimatedGuestCount} guests"
            : "guest count TBD";

        var summary =
            $"Personalized plan for \"{request.EventName}\" ({guestLine}, {request.WeddingDate:dd MMM yyyy}). " +
            $"Style: {request.WeddingStyle ?? "to be refined"}. " +
            "Standard checklist trimmed for booked services and couple priorities; custom tasks added for follow-ups from discovery.";

        var exclude = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.ServicesAlreadyBooked))
        {
            var booked = request.ServicesAlreadyBooked.ToLowerInvariant();
            if (booked.Contains("photo")) exclude.Add("Book photographer and videographer");
            if (booked.Contains("cater")) exclude.Add("Book catering and tasting session");
            if (booked.Contains("venue")) exclude.Add("Book ceremony and reception venue");
            if (booked.Contains("flor")) exclude.Add("Book florist and decor stylist");
        }

        if (request.EstimatedGuestCount is < 80)
        {
            exclude.Add("Arrange bachelor and bachelorette events");
        }

        var additional = new List<ProposedTaskItem>
        {
            new(
                "Confirm cultural ceremony requirements with family",
                request.CulturalOrReligiousNotes,
                null,
                "High"),
            new(
                "Share updated vendor shortlist with couple",
                "Align with must-haves captured in the discovery brief.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                "Medium"),
        };

        return new PersonalizedChecklistPlanResult(summary, exclude, additional, IsSimulated: true);
    }
}
