using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrderHistories;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.ServiceOrders;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class ServiceOrderWithItemsTests(OficinaApiFactory factory, ITestOutputHelper output)
    : IClassFixture<OficinaApiFactory>
{
    private const string Route = "/api/v1/service-orders/with-items";

    private static int _documentCounter = 700_000;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Should_return_201_with_location_and_open_order_in_AwaitingApproval()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request(partQuantity: 2));
        Log("Open order with items", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(nameof(ServiceOrderStatus.AwaitingApproval), order.Status);
        Assert.Null(order.MechanicId);
        Assert.Null(order.CheckList);
        Assert.Single(order.WorkshopServices);
        Assert.Equal(2, Assert.Single(order.Parts).QuantityUsed);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/service-orders/{order.Id}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var getResponse = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Should_record_four_history_entries_reserve_stock_and_send_a_single_email()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request(partQuantity: 3));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var history = (await _client.GetFromJsonAsync<List<ServiceOrderHistoryResponse>>(
            $"/api/v1/service-order-history/service-order/{order.Id}"))!;
        Assert.Equal(
            ["Created", "Received", "InDiagnosis", "AwaitingApproval"],
            history.OrderBy(entry => entry.CreatedAt).Select(entry => entry.StatusName));

        Assert.Equal(7, (await GetStockAsync(data.Part.Id)).Quantity);

        var emails = EmailsTo(data.Customer.Email);
        var email = Assert.Single(emails);
        Assert.Contains("OS aberta: orçamento aguardando aprovação", email.Subject);
        Assert.Contains(order.Id.ToString(), email.Body);
        Assert.Contains(data.Vehicle.Plate, email.Body);
        Assert.Contains(data.WorkshopService.Name, email.Body);
        Assert.Contains(data.Part.Name, email.Body);
    }

    [Fact]
    public async Task Should_follow_the_existing_flow_after_opening()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var openResponse = await _client.PostAsJsonAsync(Route, data.Request());
        var order = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var approve = await _client.PostAsync($"/api/v1/service-orders/{order.Id}/approve", content: null);
        Log("Approve", approve);
        Assert.Equal(nameof(ServiceOrderStatus.InExecution), (await approve.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status);

        var finalize = await _client.PostAsync($"/api/v1/service-orders/{order.Id}/finalize", content: null);
        Log("Finalize", finalize);
        Assert.Equal(nameof(ServiceOrderStatus.Finalized), (await finalize.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status);

        var deliver = await _client.PostAsync($"/api/v1/service-orders/{order.Id}/deliver", content: null);
        Log("Deliver", deliver);
        Assert.Equal(nameof(ServiceOrderStatus.Delivered), (await deliver.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status);
    }

    [Fact]
    public async Task Should_return_400_when_customer_is_disabled()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        (await _client.DeleteAsync($"/api/v1/customers/{data.Customer.Id}")).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync(Route, data.Request());

        await AssertRejectedAsync(response, HttpStatusCode.BadRequest,
            "The customer with the informed document has been disabled.", data);
    }

    [Fact]
    public async Task Should_return_409_when_plate_belongs_to_another_customer()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var other = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request() with
        {
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest(other.Vehicle.Plate)
        });

        await AssertRejectedAsync(response, HttpStatusCode.Conflict,
            "The vehicle with the informed plate belongs to another customer.", data);
    }

    [Fact]
    public async Task Should_reuse_existing_customer_and_vehicle()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        Assert.Equal(data.Customer.Id, order.CustomerId);
        Assert.Equal(data.Vehicle.Id, order.VehicleId);
        var vehicles = (await _client.GetFromJsonAsync<PagedResponse<VehicleResponse>>(
            $"/api/v1/vehicles?customerId={data.Customer.Id}&page=1&pageSize=100"))!;
        Assert.Equal(data.Vehicle.Id, Assert.Single(vehicles.Items).Id);
    }

    [Fact]
    public async Task Should_register_customer_and_vehicle_when_they_do_not_exist()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var sequence = Interlocked.Increment(ref _documentCounter);
        var document = TestDocuments.ValidCpf(sequence);
        var plate = $"NEW{sequence % 10000:0000}";
        var email = $"with-items.new.{sequence}@example.com";

        var response = await _client.PostAsJsonAsync(Route, data.Request() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest(
                document, $"New Customer {sequence}", email, "+5511988880000"),
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest(plate, "Ford", "Ka", 2019)
        });
        Log("Open order registering customer and vehicle", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var customer = (await _client.GetFromJsonAsync<CustomerResponse>($"/api/v1/customers/{order.CustomerId}"))!;
        Assert.Equal(document, customer.Document);
        Assert.Equal(email, customer.Email);
        var vehicle = (await _client.GetFromJsonAsync<VehicleResponse>($"/api/v1/vehicles/{order.VehicleId}"))!;
        Assert.Equal(customer.Id, vehicle.CustomerId);
        Assert.Equal("Ka", vehicle.Model);
        Assert.Single(EmailsTo(email));
    }

    [Fact]
    public async Task Should_return_400_and_register_nothing_when_new_customer_has_no_data()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var sequence = Interlocked.Increment(ref _documentCounter);
        var document = TestDocuments.ValidCpf(sequence);

        var response = await _client.PostAsJsonAsync(Route, data.Request() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest(document)
        });

        Log("Rejected (expected 400)", response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.StartsWith("Customer name is required.", problem.RootElement.GetProperty("detail").GetString());
        var createCustomer = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = $"Later Customer {sequence}",
            email = $"with-items.later.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document
        });
        Assert.Equal(HttpStatusCode.Created, createCustomer.StatusCode);
    }

    [Fact]
    public async Task Should_return_400_when_workshop_service_does_not_exist()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var missing = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(Route, data.Request() with { WorkshopServiceIds = [missing] });

        await AssertRejectedAsync(response, HttpStatusCode.BadRequest,
            $"Workshop service '{missing}' was not found.", data);
    }

    [Fact]
    public async Task Should_return_400_when_workshop_service_list_is_empty()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request() with { WorkshopServiceIds = [] });

        await AssertRejectedAsync(response, HttpStatusCode.BadRequest,
            "At least one workshop service is required.", data);
    }

    [Fact]
    public async Task Should_return_400_when_part_does_not_exist()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);
        var missing = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(Route, data.Request() with
        {
            Parts = [new OpenServiceOrderWithItemsPartRequest(missing, 1)]
        });

        await AssertRejectedAsync(response, HttpStatusCode.BadRequest,
            $"Part '{missing}' was not found.", data);
    }

    [Fact]
    public async Task Should_return_400_when_item_is_repeated()
    {
        var data = await CreateDependenciesAsync(initialStock: 10);

        var response = await _client.PostAsJsonAsync(Route, data.Request() with
        {
            Parts =
            [
                new OpenServiceOrderWithItemsPartRequest(data.Part.Id, 1),
                new OpenServiceOrderWithItemsPartRequest(data.Part.Id, 1)
            ]
        });

        await AssertRejectedAsync(response, HttpStatusCode.BadRequest,
            "A part cannot be repeated in the service order.", data);
    }

    [Fact]
    public async Task Should_return_409_with_reason_when_stock_is_insufficient()
    {
        var data = await CreateDependenciesAsync(initialStock: 3);

        var response = await _client.PostAsJsonAsync(Route, data.Request(partQuantity: 5));

        await AssertRejectedAsync(response, HttpStatusCode.Conflict,
            $"Insufficient stock for part '{data.Part.Id}'. Available: 3, requested: 5.", data);
        Assert.Equal(3, (await GetStockAsync(data.Part.Id)).Quantity);
    }

    [Fact]
    public async Task OpenApi_should_publish_the_new_endpoint_and_request_schema()
    {
        await AuthenticateAsync();
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.True(paths.TryGetProperty(Route, out var path));
        Assert.True(path.TryGetProperty("post", out var post));
        Assert.True(post.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(schemas.TryGetProperty(nameof(OpenServiceOrderWithItemsRequest), out _));
        Assert.True(schemas.TryGetProperty(nameof(OpenServiceOrderWithItemsPartRequest), out _));
        Assert.True(schemas.TryGetProperty(nameof(OpenServiceOrderWithItemsCustomerRequest), out _));
        Assert.True(schemas.TryGetProperty(nameof(OpenServiceOrderWithItemsVehicleRequest), out _));
    }

    private async Task AssertRejectedAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedDetail,
        TestData data)
    {
        Log($"Rejected (expected {(int)expectedStatus})", response);
        Assert.Equal(expectedStatus, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedDetail, problem.RootElement.GetProperty("detail").GetString());

        var orders = (await _client.GetFromJsonAsync<List<ServiceOrderListItemResponse>>("/api/v1/service-orders"))!;
        Assert.DoesNotContain(orders, order => order.CustomerId == data.Customer.Id);
        Assert.Empty(EmailsTo(data.Customer.Email));
    }

    private async Task<TestData> CreateDependenciesAsync(int initialStock)
    {
        await AuthenticateAsync();
        var sequence = Interlocked.Increment(ref _documentCounter);

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = $"With Items Customer {sequence}",
            email = $"with-items.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"WIT{sequence % 10000:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"With Items Service {sequence}",
            description = "Service used to open orders with items",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var partResponse = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = $"With Items Part {sequence}",
            code = $"WI-{Guid.NewGuid():N}"[..20],
            unitPrice = 10m,
            kind = 1
        });
        partResponse.EnsureSuccessStatusCode();
        var part = (await partResponse.Content.ReadFromJsonAsync<PartResponse>())!;

        var entryResponse = await _client.PutAsJsonAsync(
            $"/api/v1/stocks/stocks-part/{part.Id}/entries", new { quantity = initialStock });
        entryResponse.EnsureSuccessStatusCode();

        return new TestData(customer, vehicle, workshopService, part);
    }

    private async Task<StockResponse> GetStockAsync(Guid partId)
    {
        var stocks = (await _client.GetFromJsonAsync<PagedResponse<StockResponse>>("/api/v1/stocks?page=1&pageSize=100"))!;
        return stocks.Items.Single(stock => stock.PartId == partId);
    }

    private static List<OficinaApiFactory.SentEmail> EmailsTo(string recipient) =>
        OficinaApiFactory.FakeEmailTransport.SentEmails.Where(email => email.Recipient == recipient).ToList();

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

    private sealed record TestData(
        CustomerResponse Customer,
        VehicleResponse Vehicle,
        WorkshopServiceResponse WorkshopService,
        PartResponse Part)
    {
        public OpenServiceOrderWithItemsRequest Request(int partQuantity = 1) =>
            new(
                new OpenServiceOrderWithItemsCustomerRequest(Customer.Document),
                new OpenServiceOrderWithItemsVehicleRequest(Vehicle.Plate),
                "Revisao completa",
                [WorkshopService.Id],
                [new OpenServiceOrderWithItemsPartRequest(Part.Id, partQuantity)]);
    }
}
