using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.ServiceOrders;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class StockTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Entry_should_increase_stock_quantity()
    {
        var part = await CreatePartAsync("STOCK-ENTRY");

        var response = await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 10 });
        Log("Entry of 10 units into a zeroed stock", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = (await response.Content.ReadFromJsonAsync<StockResponse>())!;

        Assert.Equal(10, stock.Quantity);
    }

    [Fact]
    public async Task Consume_should_reduce_stock_quantity()
    {
        var part = await CreatePartAsync("STOCK-CONSUME");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 10 });

        var response = await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/consumptions", new { quantity = 4 });
        Log("Consume 4 units (10 -> 6)", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = (await response.Content.ReadFromJsonAsync<StockResponse>())!;

        Assert.Equal(6, stock.Quantity);
    }

    [Fact]
    public async Task Consume_should_reject_when_stock_is_insufficient()
    {
        var part = await CreatePartAsync("STOCK-INSUFF");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 3 });

        var response = await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/consumptions", new { quantity = 10 });
        Log("Consume 10 units with only 3 in stock (expected: rejected)", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Adjust_should_set_absolute_stock_quantity()
    {
        var part = await CreatePartAsync("STOCK-ADJUST");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 10 });

        var response = await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/adjustments", new { quantity = 25 });
        Log("Absolute adjustment to 25 units (regardless of the previous value)", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = (await response.Content.ReadFromJsonAsync<StockResponse>())!;

        Assert.Equal(25, stock.Quantity);
    }

    [Fact]
    public async Task Service_order_should_deduct_stock_when_part_is_attached()
    {
        var (customer, vehicle, mechanic, workshopService) = await CreateOrderDependenciesAsync();
        var part = await CreatePartAsync("STOCK-OS-DEDUCT");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 10 });

        var serviceOrder = await OpenServiceOrderAsync(customer.Id, vehicle.Id);
        await AdvanceToInDiagnosisAsync(serviceOrder.Id, mechanic.Id);

        var attachPartResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            parts = new[] { new { partId = part.Id, quantity = 3 } }
        });
        Assert.Equal(HttpStatusCode.OK, attachPartResponse.StatusCode);

        var stockAfterAttach = await GetStockByPartIdAsync(part.Id);
        Log($"Stock after attaching 3 units of the part to the order (10 -> {stockAfterAttach.Quantity})", attachPartResponse);
        Assert.Equal(7, stockAfterAttach.Quantity);

        // The deduction happens the moment the part is attached to the order (during Update, still in
        // InDiagnosis/AwaitingApproval) - there is no additional deduction on approve/finalize/deliver.
        await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        });
        var approveResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var finalizeResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/finalize", content: null);
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var deliverResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/deliver", content: null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);

        var stockAfterDelivery = await GetStockByPartIdAsync(part.Id);
        Log($"Stock after approve/finalize/deliver (should remain at {stockAfterAttach.Quantity})", deliverResponse);
        Assert.Equal(7, stockAfterDelivery.Quantity);
    }

    [Fact]
    public async Task Service_order_should_return_stock_when_cancelled()
    {
        var (customer, vehicle, mechanic, workshopService) = await CreateOrderDependenciesAsync();
        var part = await CreatePartAsync("STOCK-OS-CANCEL");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 10 });

        var serviceOrder = await OpenServiceOrderAsync(customer.Id, vehicle.Id);
        await AdvanceToInDiagnosisAsync(serviceOrder.Id, mechanic.Id);

        await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            parts = new[] { new { partId = part.Id, quantity = 3 } }
        });
        var stockAfterAttach = await GetStockByPartIdAsync(part.Id);
        Assert.Equal(7, stockAfterAttach.Quantity);

        var awaitingApprovalResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        });
        Assert.Equal(HttpStatusCode.OK, awaitingApprovalResponse.StatusCode);

        var cancelResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/cancel", content: null);
        Log("Cancel order with 3 units of the part attached", cancelResponse);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelledOrder = await cancelResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>();
        Assert.Equal(ServiceOrderStatus.Rejected, cancelledOrder!.Status);

        var stockAfterCancel = await GetStockByPartIdAsync(part.Id);
        Log($"Stock after cancelling the order (7 -> {stockAfterCancel.Quantity}, expected back to 10)", cancelResponse);
        Assert.Equal(10, stockAfterCancel.Quantity);
    }

    // Item 10 of docs/analise-gaps-e-cenarios-faltantes.md: increasing the quantity of a part
    // already attached to the order, beyond what's left in stock, must be rejected with 400 - the
    // HTTP-level mirror of the equivalent Application-layer test (ServiceOrderContractTests).
    [Fact]
    public async Task Service_order_should_reject_increasing_attached_part_quantity_beyond_available_stock()
    {
        var (customer, vehicle, mechanic, _) = await CreateOrderDependenciesAsync();
        var part = await CreatePartAsync("STOCK-OS-INCREASE");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 5 });

        var serviceOrder = await OpenServiceOrderAsync(customer.Id, vehicle.Id);
        await AdvanceToInDiagnosisAsync(serviceOrder.Id, mechanic.Id);

        var firstAttachResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            parts = new[] { new { partId = part.Id, quantity = 3 } }
        });
        Assert.Equal(HttpStatusCode.OK, firstAttachResponse.StatusCode);
        var stockAfterFirstAttach = await GetStockByPartIdAsync(part.Id);
        Assert.Equal(2, stockAfterFirstAttach.Quantity);

        // Only 2 left in stock; asking to raise the already-attached quantity from 3 to 10 means
        // consuming 7 more, which is beyond what's available.
        var increaseResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            parts = new[] { new { partId = part.Id, quantity = 10 } }
        });
        Log("Increase attached part quantity from 3 to 10 with only 2 units left in stock (expected: 400)", increaseResponse);

        Assert.Equal(HttpStatusCode.BadRequest, increaseResponse.StatusCode);
    }

    // Item 11 of docs/analise-gaps-e-cenarios-faltantes.md: two orders sequentially disputing the
    // last unit(s) of the same part - the second one to attach it should be rejected once stock
    // is exhausted by the first.
    [Fact]
    public async Task Two_orders_disputing_the_same_part_should_reject_the_second_once_stock_is_exhausted()
    {
        var (customerA, vehicleA, mechanicA, _) = await CreateOrderDependenciesAsync();
        var (customerB, vehicleB, mechanicB, _) = await CreateOrderDependenciesAsync();
        var part = await CreatePartAsync("STOCK-OS-DISPUTE");
        await _client.PutAsJsonAsync($"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = 5 });

        var orderA = await OpenServiceOrderAsync(customerA.Id, vehicleA.Id);
        await AdvanceToInDiagnosisAsync(orderA.Id, mechanicA.Id);
        var attachToOrderAResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = orderA.Id,
            parts = new[] { new { partId = part.Id, quantity = 5 } }
        });
        Log("Order A attaches all 5 remaining units", attachToOrderAResponse);
        Assert.Equal(HttpStatusCode.OK, attachToOrderAResponse.StatusCode);

        var orderB = await OpenServiceOrderAsync(customerB.Id, vehicleB.Id);
        await AdvanceToInDiagnosisAsync(orderB.Id, mechanicB.Id);
        var attachToOrderBResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = orderB.Id,
            parts = new[] { new { partId = part.Id, quantity = 1 } }
        });
        Log("Order B tries to attach 1 unit after order A exhausted the stock (expected: 400)", attachToOrderBResponse);

        Assert.Equal(HttpStatusCode.BadRequest, attachToOrderBResponse.StatusCode);
    }

    private async Task<StockResponse> GetStockByPartIdAsync(Guid partId)
    {
        var response = await _client.GetAsync("/api/v1/stocks?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var stocks = (await response.Content.ReadFromJsonAsync<PagedResponse<StockResponse>>())!;
        return stocks.Items.Single(s => s.PartId == partId);
    }

    private async Task<ServiceOrderDetailResponse> OpenServiceOrderAsync(Guid customerId, Guid vehicleId)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId,
            vehicleId,
            description = "Stock test order"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
    }

    private async Task AdvanceToInDiagnosisAsync(Guid serviceOrderId, Guid mechanicId)
    {
        var checklistResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId,
            checkList = "Initial inspection completed"
        });
        checklistResponse.EnsureSuccessStatusCode();

        var mechanicResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId,
            mechanicId
        });
        mechanicResponse.EnsureSuccessStatusCode();
    }

    private async Task<(CustomerResponse Customer, VehicleResponse Vehicle, MechanicResponse Mechanic, WorkshopServiceResponse WorkshopService)>
        CreateOrderDependenciesAsync()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Stock Test Customer",
            email = $"stock.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"STK{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"Stock Test Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Stock Test Service {sequence}",
            description = "Service used to test stock consumption",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        return (customer, vehicle, mechanic, workshopService);
    }

    private async Task<PartResponse> CreatePartAsync(string codeSuffix)
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = $"Part {codeSuffix}",
            code = $"{codeSuffix}-{Guid.NewGuid():N}",
            unitPrice = 10m,
            kind = 1
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PartResponse>())!;
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
