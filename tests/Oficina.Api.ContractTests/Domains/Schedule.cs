using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.OrdensServico;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

// Item 15 of docs/analise-gaps-e-cenarios-faltantes.md: GET /api/v1/schedules via real HTTP
// (the underlying repository behavior is already covered by ServiceOrderRepositoryTests; this
// exercises the real endpoint end-to-end).
public sealed class ScheduleTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_without_date_should_return_orders_scheduled_within_the_next_30_days()
    {
        var order = await OpenServiceOrderAsync();

        var response = await _client.GetAsync("/api/v1/schedules");
        Log("GET /api/v1/schedules (no date filter)", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schedules = (await response.Content.ReadFromJsonAsync<List<ServiceOrderSchedulesDto>>())!;
        Assert.Contains(schedules, schedule => schedule.OrderServiceId == order.Id);
    }

    [Fact]
    public async Task List_with_todays_date_should_include_the_order_just_opened()
    {
        var order = await OpenServiceOrderAsync();
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/schedules?date={today}");
        Log($"GET /api/v1/schedules?date={today}", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schedules = (await response.Content.ReadFromJsonAsync<List<ServiceOrderSchedulesDto>>())!;
        Assert.Contains(schedules, schedule => schedule.OrderServiceId == order.Id);
    }

    [Fact]
    public async Task List_with_a_date_far_in_the_past_should_return_not_found()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/v1/schedules?date=2000-01-01");
        Log("GET /api/v1/schedules?date=2000-01-01 (expected: no schedules that far back)", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ServiceOrderDetailResponse> OpenServiceOrderAsync()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Schedule Test Customer",
            email = $"schedule.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"SCH{sequence:0000}",
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
            description = "Order used to test the schedules endpoint"
        });
        openResponse.EnsureSuccessStatusCode();
        return (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
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
