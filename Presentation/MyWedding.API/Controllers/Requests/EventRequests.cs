namespace MyWedding.API.Controllers.Requests;

/// <summary>Request DTO for creating a new wedding event.</summary>
public record CreateEventRequest(string EventName, DateTime EventDate);

/// <summary>Request DTO for setting or updating the total budget of an event.</summary>
public record SetTotalBudgetRequest(decimal TotalBudget);
