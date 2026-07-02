using MyWedding.Domain.Entities;

namespace MyWedding.Domain.Tests.Entities;

public class WeddingEventTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsEventWithFutureDate()
    {
        var eventDate = DateTime.UtcNow.AddDays(30);

        var weddingEvent = WeddingEvent.Create("Summer Wedding", eventDate, "user-123");

        Assert.NotEqual(Guid.Empty, weddingEvent.Id);
        Assert.Equal("Summer Wedding", weddingEvent.EventName);
        Assert.Equal(eventDate, weddingEvent.EventDate);
        Assert.Equal("user-123", weddingEvent.CreatedById);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string eventName)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WeddingEvent.Create(eventName, DateTime.UtcNow.AddDays(1), "user-123"));

        Assert.Equal("eventName", ex.ParamName);
    }

    [Fact]
    public void Create_WithPastDate_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WeddingEvent.Create("Wedding", DateTime.UtcNow.AddDays(-1), "user-123"));

        Assert.Equal("eventDate", ex.ParamName);
    }
}
