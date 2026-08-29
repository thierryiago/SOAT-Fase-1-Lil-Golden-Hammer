using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Mechanics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

// Item 17 of docs/analise-gaps-e-cenarios-faltantes.md: MechanicsController only had
// Application-layer (fakes) coverage; this exercises create -> update -> not-found -> delete via
// real HTTP requests, matching the pattern already used for Customer/Vehicle/Part.
public sealed class MechanicTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_should_register_a_new_mechanic()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = "John Wrench" });
        Log("Create mechanic", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var mechanic = (await response.Content.ReadFromJsonAsync<MechanicResponse>())!;
        Assert.Equal("John Wrench", mechanic.Name);
    }

    [Fact]
    public async Task GetById_should_return_not_found_for_unknown_mechanic()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/v1/mechanics/{Guid.NewGuid()}");
        Log("Get unknown mechanic", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_change_mechanic_name()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = "Old Name" });
        var mechanic = (await createResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/mechanics/{mechanic.Id}", new { name = "New Name" });
        Log("Update mechanic name", updateResponse);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = (await updateResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task Update_should_return_not_found_for_unknown_mechanic()
    {
        await AuthenticateAsync();

        var response = await _client.PutAsJsonAsync($"/api/v1/mechanics/{Guid.NewGuid()}", new { name = "Anyone" });
        Log("Update unknown mechanic", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_should_soft_delete_an_existing_mechanic()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = "To Be Deleted" });
        var mechanic = (await createResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/mechanics/{mechanic.Id}");
        Log("Delete mechanic", deleteResponse);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/mechanics/{mechanic.Id}");
        Log("Get mechanic after delete (soft-deleted, should no longer be visible)", getResponse);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_not_found_for_unknown_mechanic()
    {
        await AuthenticateAsync();

        var response = await _client.DeleteAsync($"/api/v1/mechanics/{Guid.NewGuid()}");
        Log("Delete unknown mechanic", response);

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
