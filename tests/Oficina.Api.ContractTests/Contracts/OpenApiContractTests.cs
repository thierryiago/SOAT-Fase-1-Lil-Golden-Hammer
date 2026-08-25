using Oficina.Api.ContractTests.Infrastructure;
using System.Text.Json;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class OpenApiContractTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_should_publish_only_versioned_administrative_paths_and_dtos()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas").EnumerateObject().Select(schema => schema.Name).ToList();

        Assert.All(paths, path => Assert.StartsWith("/api/v1/", path));
        Assert.Contains("ServiceOrderListItemResponse", schemas);
        Assert.Contains("ServiceOrderDetailResponse", schemas);
        Assert.Contains("WorkshopServiceExecutionTimeResponse", schemas);
        Assert.DoesNotContain("ServiceOrder", schemas);
        Assert.DoesNotContain("ServiceOrderHistory", schemas);
    }
}
