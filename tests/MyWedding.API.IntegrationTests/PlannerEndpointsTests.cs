using System.Net;

namespace MyWedding.API.IntegrationTests;

public class PlannerEndpointsTests : IClassFixture<MyWeddingApiFactory>
{
    private readonly MyWeddingApiFactory _factory;

    public PlannerEndpointsTests(MyWeddingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_TaskTemplates_WithoutAuth_Returns_Unauthorized()
    {
        using var client = _factory.CreateClient();
        client.ClearAuthentication();

        var response = await client.GetAsync("/api/planner/task-templates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_TaskTemplates_AsActivePlanner_Returns_Ok()
    {
        await IntegrationTestSeed.SeedActivePlannerAsync(_factory.Services);

        using var client = _factory.CreateClient();
        client.AuthenticateAs(IntegrationTestSeed.ActivePlannerUserId, "planner");

        var response = await client.GetAsync("/api/planner/task-templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_PlannerOverview_AsActivePlanner_Returns_Ok()
    {
        await IntegrationTestSeed.SeedActivePlannerAsync(_factory.Services);

        using var client = _factory.CreateClient();
        client.AuthenticateAs(IntegrationTestSeed.ActivePlannerUserId, "planner");

        var response = await client.GetAsync("/api/planner/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_TaskTemplates_WithExpiredSubscription_Returns_PaymentRequired()
    {
        await IntegrationTestSeed.SeedExpiredPlannerAsync(_factory.Services);

        using var client = _factory.CreateClient();
        client.AuthenticateAs(IntegrationTestSeed.ExpiredPlannerUserId, "planner");

        var response = await client.GetAsync("/api/planner/task-templates");

        Assert.Equal((HttpStatusCode)402, response.StatusCode);
    }

    [Fact]
    public async Task Get_PlannerOverview_WithExpiredSubscription_Returns_Ok()
    {
        await IntegrationTestSeed.SeedExpiredPlannerAsync(_factory.Services);

        using var client = _factory.CreateClient();
        client.AuthenticateAs(IntegrationTestSeed.ExpiredPlannerUserId, "planner");

        var response = await client.GetAsync("/api/planner/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
