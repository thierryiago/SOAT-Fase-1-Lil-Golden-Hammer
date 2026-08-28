using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Contracts;

// Covers docs/analise-gaps-e-cenarios-faltantes.md, section 2.1 (items 1-5): the security/authorization
// boundary of the API as it stands today. BudgetsController and NotificationsController are
// intentionally anonymous (confirmed 2026-08-27, so the customer can check a budget/receive
// notifications without an administrative credential); MetricsController and ScheduleController are
// administrative and require a token; and there is no RBAC at all - any valid token can call any
// administrative endpoint. These tests lock in that behavior as a guard against accidental regression
// in either direction.
public sealed class AnonymousEndpointsTests(OficinaApiFactory factory, ITestOutputHelper output)
    : IClassFixture<OficinaApiFactory>
{
    // Item 1 - GET /api/v1/budgets and GET /api/v1/budgets/{id} without a token.
    [Fact]
    public async Task Budgets_list_should_work_without_a_token_by_design()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/budgets?page=1&pageSize=20");
        Log("GET /api/v1/budgets (no Authorization header) - intentionally anonymous", response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Budget_by_id_should_return_not_found_not_unauthorized_without_a_token()
    {
        using var client = factory.CreateClient();

        // Unknown id on purpose: the point here is only to confirm the route does not require
        // authentication (401 would mean BudgetsController regained an [Authorize] attribute).
        var response = await client.GetAsync($"/api/v1/budgets/{Guid.NewGuid()}");
        Log("GET /api/v1/budgets/{id} (no Authorization header) - intentionally anonymous", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Item 2 - POST /api/v1/notifications/email without a token.
    [Fact]
    public async Task Send_email_notification_should_work_without_a_token_by_design()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/notifications/email",
            new { email = "anonymous.notification@example.com" });
        Log("POST /api/v1/notifications/email (no Authorization header) - intentionally anonymous", response);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Item 3 - GET /api/v1/metrics/... without a token must be 401 (administrative route).
    [Fact]
    public async Task Metrics_route_without_token_should_return_unauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/metrics/workshop-service/execution-time");
        Log("GET /api/v1/metrics/workshop-service/execution-time (no Authorization header)", response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Item 4 - GET /api/v1/schedules without a token must be 401 (administrative route).
    [Fact]
    public async Task Schedules_route_without_token_should_return_unauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/schedules");
        Log("GET /api/v1/schedules (no Authorization header)", response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Item 5 - there is no RBAC: any token issued by the anonymous /auth/token endpoint (no
    // credentials, no role claims) can perform any administrative operation, including a
    // destructive one. This documents a conscious MVP limitation and guards against a future
    // change silently adding roles without updating this test.
    [Fact]
    public async Task Any_valid_token_should_be_able_to_perform_any_administrative_operation()
    {
        using var client = factory.CreateClient();

        var tokenResponse = await client.PostAsync("/api/v1/auth/token", content: null);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Log("Issue token (no credentials/role claims required)", tokenResponse);

        // Opening a service order, deleting a customer, adjusting stock... any administrative
        // action is reachable with the exact same token. Here: create then delete a mechanic.
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/mechanics",
            new { name = $"RBAC probe mechanic {Guid.NewGuid():N}" });
        Log("Create mechanic with the generic token", createResponse);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var mechanicId = (await createResponse.Content.ReadFromJsonAsync<MechanicIdOnly>())!.Id;

        var deleteResponse = await client.DeleteAsync($"/api/v1/mechanics/{mechanicId}");
        Log("Delete (destructive, administrative) the same mechanic with the same generic token", deleteResponse);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private sealed record MechanicIdOnly(Guid Id);

    private void Log(string scenario, HttpResponseMessage response) =>
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
}
