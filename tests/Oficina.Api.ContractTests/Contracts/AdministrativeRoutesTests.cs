using System.Net;
using Oficina.Api.ContractTests.Infrastructure;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class AdministrativeRoutesTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    public static TheoryData<string> Resources => new()
    {
        "customers", "vehicles", "mechanics", "services", "parts", "stocks",
        "service-orders", "service-order-history"
    };

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Administrative_resource_should_only_be_available_under_v1(string resource)
    {
        var versioned = await _client.GetAsync($"/api/v1/{resource}");
        var legacy = await _client.GetAsync($"/api/{resource}");

        Assert.NotEqual(HttpStatusCode.NotFound, versioned.StatusCode);
        Assert.True((int)versioned.StatusCode < 500, $"Versioned route returned {versioned.StatusCode}.");
        Assert.Equal(HttpStatusCode.NotFound, legacy.StatusCode);
    }

    [Fact]
    public async Task Infrastructure_routes_should_remain_unversioned()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}
