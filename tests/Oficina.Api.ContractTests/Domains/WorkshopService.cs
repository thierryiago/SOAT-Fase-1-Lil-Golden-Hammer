using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.WorkshopServices;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

// Item 17 of docs/analise-gaps-e-cenarios-faltantes.md: WorkshopServicesController only had
// Application-layer (fakes) coverage; this exercises create -> conflict -> not-found -> update ->
// delete via real HTTP requests, matching the pattern already used for Customer/Vehicle/Part.
public sealed class WorkshopServiceTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_should_register_a_new_workshop_service()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Oil Change {Guid.NewGuid():N}",
            description = "Full oil and filter change",
            unitPrice = 150m,
            estimatedDurationMinutes = 40
        });
        Log("Create workshop service", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_duplicate_name()
    {
        await AuthenticateAsync();

        var name = $"Duplicate Service {Guid.NewGuid():N}";
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name,
            description = "First registration",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name,
            description = "Second registration with the same name",
            unitPrice = 200m,
            estimatedDurationMinutes = 60
        });
        Log("Create workshop service with duplicate name", duplicateResponse);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_should_return_not_found_for_unknown_service()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/v1/workshop-services/{Guid.NewGuid()}");
        Log("Get unknown workshop service", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_change_service_data()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Brake Inspection {Guid.NewGuid():N}",
            description = "Inspect brake pads and discs",
            unitPrice = 80m,
            estimatedDurationMinutes = 20
        });
        var service = (await createResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/workshop-services/{service.Id}", new
        {
            name = service.Name,
            description = "Inspect and replace brake pads and discs",
            unitPrice = 220m,
            estimatedDurationMinutes = 60
        });
        Log("Update workshop service", updateResponse);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = (await updateResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;
        Assert.Equal(220m, updated.UnitPrice);
        Assert.Equal(60, updated.EstimatedDurationMinutes);
    }

    [Fact]
    public async Task Update_should_return_not_found_for_unknown_service()
    {
        await AuthenticateAsync();

        var response = await _client.PutAsJsonAsync($"/api/v1/workshop-services/{Guid.NewGuid()}", new
        {
            name = "Anything",
            description = "Anything",
            unitPrice = 10m,
            estimatedDurationMinutes = 10
        });
        Log("Update unknown workshop service", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_should_soft_delete_an_existing_service()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"To Be Deleted {Guid.NewGuid():N}",
            description = "Will be soft-deleted",
            unitPrice = 50m,
            estimatedDurationMinutes = 15
        });
        var service = (await createResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/workshop-services/{service.Id}");
        Log("Delete workshop service", deleteResponse);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/workshop-services/{service.Id}");
        Log("Get workshop service after delete (soft-deleted, should no longer be visible)", getResponse);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_not_found_for_unknown_service()
    {
        await AuthenticateAsync();

        var response = await _client.DeleteAsync($"/api/v1/workshop-services/{Guid.NewGuid()}");
        Log("Delete unknown workshop service", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        if (_client.DefaultRequestHeaders.Authorization is not null)
        {
            return;
        }

        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void Log(string scenario, HttpResponseMessage response) =>
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
}
