using System.Net;

namespace MyWedding.API.IntegrationTests;

public class PublicEndpointsTests : IClassFixture<MyWeddingApiFactory>
{
    private readonly HttpClient _client;

    public PublicEndpointsTests(MyWeddingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Vendor_Categories_Returns_Ok()
    {
        var response = await _client.GetAsync("/api/vendors/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Vendors_Returns_Ok()
    {
        var response = await _client.GetAsync("/api/vendors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
