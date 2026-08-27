using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class ServiceOrderHistoryTests(OficinaApiFactory factory, ITestOutputHelper output)
    : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FindByServiceOrder_should_record_one_entry_per_status_in_order()
    {
        var serviceOrderId = await RunFullLifecycleAsync();

        var response = await _client.GetAsync($"/api/v1/service-order-history/service-order/{serviceOrderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = (await response.Content.ReadFromJsonAsync<List<ServiceOrderHistoryResponse>>())!;

        var statusSequence = string.Join(" -> ", history
            .OrderBy(entry => entry.CreatedAt)
            .Select(entry => entry.StatusName));
        Log($"Order history ({history.Count} entries): {statusSequence}", response);

        Assert.Equal(6, history.Count);
        Assert.All(history, entry => Assert.Equal(serviceOrderId, entry.ServiceOrderId));
        Assert.Equal(
            new[] { "Received", "InDiagnosis", "AwaitingApproval", "InExecution", "Finalized", "Delivered" },
            history.OrderBy(entry => entry.CreatedAt).Select(entry => entry.StatusName));
    }

    [Fact]
    public async Task FindByServiceOrder_should_return_empty_for_order_without_transitions()
    {
        await AuthenticateAsync();
        var (customer, vehicle, _, _) = await CreateOrderDependenciesAsync();

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Freshly opened order, no status transitions yet"
        });
        openResponse.EnsureSuccessStatusCode();
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var response = await _client.GetAsync($"/api/v1/service-order-history/service-order/{serviceOrder.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = (await response.Content.ReadFromJsonAsync<List<ServiceOrderHistoryResponse>>())!;
        Log($"History of a freshly opened order ({history.Count} entries, expected 0)", response);

        Assert.Empty(history);
    }

    [Fact]
    public async Task FindAll_should_include_entries_from_a_completed_order()
    {
        var serviceOrderId = await RunFullLifecycleAsync();

        var response = await _client.GetAsync("/api/v1/service-order-history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = (await response.Content.ReadFromJsonAsync<List<ServiceOrderHistoryResponse>>())!;
        var entriesForThisOrder = history.Count(entry => entry.ServiceOrderId == serviceOrderId);
        Log($"Overall history contains {entriesForThisOrder} entries for this order (expected 6)", response);

        Assert.Equal(6, entriesForThisOrder);
    }

    private async Task<Guid> RunFullLifecycleAsync()
    {
        await AuthenticateAsync();
        var (customer, vehicle, mechanic, workshopService) = await CreateOrderDependenciesAsync();

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order to validate the status history"
        });
        openResponse.EnsureSuccessStatusCode();
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            checkList = "Initial inspection completed"
        })).EnsureSuccessStatusCode();

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            mechanicId = mechanic.Id
        })).EnsureSuccessStatusCode();

        (await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        })).EnsureSuccessStatusCode();

        (await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/approve", content: null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/finalize", content: null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/deliver", content: null)).EnsureSuccessStatusCode();

        return serviceOrder.Id;
    }

    private async Task<(CustomerResponse Customer, VehicleResponse Vehicle, MechanicResponse Mechanic, WorkshopServiceResponse WorkshopService)>
        CreateOrderDependenciesAsync()
    {
        var sequence = Interlocked.Increment(ref _documentCounter);

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "History Test Customer",
            email = $"history.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = sequence.ToString().PadLeft(11, '0')
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"HST{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"History Test Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"History Test Service {sequence}",
            description = "Service used to test the status history",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        return (customer, vehicle, mechanic, workshopService);
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
