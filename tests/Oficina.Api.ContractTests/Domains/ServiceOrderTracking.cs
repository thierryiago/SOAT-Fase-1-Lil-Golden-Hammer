using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

// Design confirmed for the client-facing tracking endpoint (docs/analise-gaps-e-cenarios-faltantes.md,
// item A / 1.1): GET /api/v1/service-orders/{id}/track?document={cpfOuCnpj}, anonymous, identifying the
// customer by service order id + CPF/CNPJ. It does not exist yet - this file only documents the expected
// contract with tests. DO NOT implement the endpoint here; these tests are deliberately red until it is.
public sealed class ServiceOrderTrackingTests(OficinaApiFactory factory, ITestOutputHelper output)
    : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    // DELIBERATELY RED: GET /api/v1/service-orders/{id}/track?document=... does not exist yet.
    // Once implemented, this must return 200 with the order's status when id + document match.
    [Fact]
    public async Task Track_should_return_order_status_when_document_matches_without_a_token()
    {
        var ctx = await OpenOrderForCustomerAsync();

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync(
            $"/api/v1/service-orders/{ctx.OrderId}/track?document={ctx.Document}");
        Log("Track own order by id + correct document (no Authorization header sent)", response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // DELIBERATELY RED today only in the sense that the endpoint returns 404 for every call
    // (route missing) - once implemented, this must keep returning 404 for a wrong document,
    // without leaking whether the order exists.
    [Fact]
    public async Task Track_should_return_not_found_when_document_does_not_match_the_order_customer()
    {
        var ctx = await OpenOrderForCustomerAsync();
        var wrongDocument = TestDocuments.ValidCpf(Interlocked.Increment(ref _documentCounter) + 900_000);

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync(
            $"/api/v1/service-orders/{ctx.OrderId}/track?document={wrongDocument}");
        Log("Track existing order with the wrong document (must not leak that the order exists)", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Track_should_return_not_found_for_unknown_order_id()
    {
        var document = TestDocuments.ValidCpf(Interlocked.Increment(ref _documentCounter) + 900_000);

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync(
            $"/api/v1/service-orders/{Guid.NewGuid()}/track?document={document}");
        Log("Track an order id that does not exist", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELIBERATELY RED: confirms the endpoint stays anonymous (no Authorization header, no 401)
    // once implemented. Today it 404s regardless of the header because the route does not exist.
    [Fact]
    public async Task Track_should_not_require_an_authorization_header()
    {
        var ctx = await OpenOrderForCustomerAsync();

        using var anonymousClient = factory.CreateClient();
        Assert.Null(anonymousClient.DefaultRequestHeaders.Authorization);

        var response = await anonymousClient.GetAsync(
            $"/api/v1/service-orders/{ctx.OrderId}/track?document={ctx.Document}");
        Log("Track without ever sending an Authorization header", response);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record TrackingContext(Guid OrderId, string Document);

    private async Task<TrackingContext> OpenOrderForCustomerAsync()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var document = TestDocuments.ValidCpf(sequence);

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Tracking Customer",
            email = $"tracking.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"TRK{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order used to test the future client-facing tracking endpoint"
        });
        openResponse.EnsureSuccessStatusCode();
        var order = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        return new TrackingContext(order.Id, document);
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
