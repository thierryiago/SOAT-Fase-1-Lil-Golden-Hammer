using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Metrics;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class MetricsContractTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

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

    // Item 16 of docs/analise-gaps-e-cenarios-faltantes.md: runs a real order through
    // InExecution -> Finalized via HTTP and confirms the metric reflects an actually-calculated
    // duration (not just that the route responds). The wall-clock gap between the two
    // transitions in a fast test run is tiny, so this asserts the shape of a real calculation
    // (non-null, non-negative) plus that the estimated duration matches what was registered,
    // rather than pinning an exact number of minutes.
    [Fact]
    public async Task Workshop_service_execution_time_should_reflect_a_real_finalized_order()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        var token = await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Metrics Customer",
            email = $"metrics.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"MTR{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"Metrics Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Metrics Service {sequence}",
            description = "Service used to validate the execution-time metric end-to-end",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order used to validate the execution-time metric end-to-end"
        });
        openResponse.EnsureSuccessStatusCode();
        var order = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = order.Id,
            checkList = "Initial inspection completed"
        })).EnsureSuccessStatusCode();

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = order.Id,
            mechanicId = mechanic.Id
        })).EnsureSuccessStatusCode();

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = order.Id,
            workshopServiceIds = new[] { workshopService.Id }
        })).EnsureSuccessStatusCode();

        (await _client.PostAsync($"/api/v1/service-orders/{order.Id}/approve", content: null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/v1/service-orders/{order.Id}/finalize", content: null)).EnsureSuccessStatusCode();

        var metricsResponse = await _client.GetAsync("/api/v1/metrics/workshop-service/execution-time");
        metricsResponse.EnsureSuccessStatusCode();
        var metrics = (await metricsResponse.Content.ReadFromJsonAsync<List<WorkshopServiceExecutionTimeResponse>>())!;
        var metric = metrics.Single(item => item.Id == workshopService.Id);

        Assert.Equal(30, metric.EstimatedTimeMinutes);
        Assert.NotNull(metric.AverageTimeMinutes);
        Assert.True(metric.AverageTimeMinutes >= 0m);
    }
}
