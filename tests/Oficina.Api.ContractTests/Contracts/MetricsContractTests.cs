using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class MetricsContractTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Workshop_service_execution_time_should_be_available_under_v1_metrics_route()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        var token = await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var response = await _client.GetAsync("/api/v1/metrics/workshop-service/execution-time");

        Assert.True(
            response.IsSuccessStatusCode,
            await response.Content.ReadAsStringAsync());
    }
}
