using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWedding.SharedKernel.Interfaces;

namespace MyWedding.Infrastructure.Services;

public class OpenAiCopilotService : IAiCopilotService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiCopilotService> _logger;

    public OpenAiCopilotService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiCopilotService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InquiryEmailDraftResult> DraftInquiryEmailAsync(
        InquiryEmailDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI:ApiKey is not configured; returning simulated inquiry draft.");
            return await BuildFallbackInquiryDraftAsync(request, cancellationToken);
        }

        var requirements = request.ServiceRequirements is { Count: > 0 }
            ? string.Join(", ", request.ServiceRequirements)
            : "standard wedding package";

        var systemPrompt =
            "You are a professional wedding planner in Sri Lanka drafting vendor inquiry emails. " +
            "Respond with JSON only: {\"subject\":\"...\",\"body\":\"...\"}. " +
            "Body should be warm, professional, 4-6 short paragraphs, plain text with line breaks.";

        var userPrompt =
            $"Planner: {request.PlannerName}\n" +
            $"Vendor: {request.VendorBusinessName} ({request.VendorCategory})\n" +
            $"Event: {request.EventName}\n" +
            $"Wedding date: {request.WeddingDate:yyyy-MM-dd}\n" +
            $"Venue: {request.Venue ?? "TBD"}\n" +
            $"Budget LKR: {(request.BudgetLkr.HasValue ? request.BudgetLkr.Value.ToString("N0") : "flexible")}\n" +
            $"Style: {request.StyleNotes ?? "elegant, candid"}\n" +
            $"Requirements: {requirements}";

        try
        {
            var content = await CallChatCompletionsAsync(systemPrompt, userPrompt, jsonMode: true, cancellationToken);
            var parsed = JsonSerializer.Deserialize<InquiryEmailJson>(content, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Subject) || string.IsNullOrWhiteSpace(parsed.Body))
                throw new InvalidOperationException("OpenAI returned an invalid inquiry email payload.");

            return new InquiryEmailDraftResult(parsed.Subject.Trim(), parsed.Body.Trim(), IsSimulated: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI inquiry draft failed; using fallback.");
            return await BuildFallbackInquiryDraftAsync(request, cancellationToken);
        }
    }

    public async Task<MeetingSummaryResult> SummarizeMeetingToTasksAsync(
        MeetingSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI:ApiKey is not configured; returning simulated meeting summary.");
            return await BuildFallbackMeetingSummaryAsync(request, cancellationToken);
        }

        var systemPrompt =
            "You are a wedding planning assistant. Summarize client meetings and propose actionable tasks. " +
            "Respond with JSON only using this schema: " +
            "{\"executiveSummary\":\"string\",\"proposedTasks\":[{\"title\":\"string\",\"description\":\"string|null\"," +
            "\"suggestedDueDate\":\"yyyy-MM-dd|null\",\"priority\":\"High|Medium|Low\"}]}";

        var userPrompt =
            $"Event: {request.EventName} (id: {request.EventId})\n\n" +
            $"Meeting notes / transcript:\n{request.MeetingNotesOrTranscript}";

        try
        {
            var content = await CallChatCompletionsAsync(systemPrompt, userPrompt, jsonMode: true, cancellationToken);
            var parsed = JsonSerializer.Deserialize<MeetingSummaryJson>(content, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.ExecutiveSummary))
                throw new InvalidOperationException("OpenAI returned an invalid meeting summary payload.");

            var tasks = parsed.ProposedTasks?
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Select(t => new ProposedTaskItem(
                    t.Title!.Trim(),
                    string.IsNullOrWhiteSpace(t.Description) ? null : t.Description.Trim(),
                    ParseDueDate(t.SuggestedDueDate),
                    string.IsNullOrWhiteSpace(t.Priority) ? "Medium" : t.Priority.Trim()))
                .ToList() ?? [];

            return new MeetingSummaryResult(parsed.ExecutiveSummary.Trim(), tasks, IsSimulated: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI meeting summary failed; using fallback.");
            return await BuildFallbackMeetingSummaryAsync(request, cancellationToken);
        }
    }

    private async Task<string> CallChatCompletionsAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["OpenAI:ApiKey"]!;
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";

        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["temperature"] = 0.4,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        if (jsonMode)
            body["response_format"] = new { type = "json_object" };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error {(int)response.StatusCode}: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");

        return content;
    }

    private static DateOnly? ParseDueDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateOnly.TryParse(value, out var d) ? d : null;
    }

    private static async Task<InquiryEmailDraftResult> BuildFallbackInquiryDraftAsync(
        InquiryEmailDraftRequest request,
        CancellationToken cancellationToken) =>
        await new Mocks.MockAiCopilotService().DraftInquiryEmailAsync(request, cancellationToken);

    private static async Task<MeetingSummaryResult> BuildFallbackMeetingSummaryAsync(
        MeetingSummaryRequest request,
        CancellationToken cancellationToken) =>
        await new Mocks.MockAiCopilotService().SummarizeMeetingToTasksAsync(request, cancellationToken);

    private sealed class InquiryEmailJson
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }

    private sealed class MeetingSummaryJson
    {
        public string? ExecutiveSummary { get; set; }
        public List<ProposedTaskJson>? ProposedTasks { get; set; }
    }

    private sealed class ProposedTaskJson
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? SuggestedDueDate { get; set; }
        public string? Priority { get; set; }
    }
}
