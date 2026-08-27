using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.ServiceOrders;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class ServiceOrderTests(OficinaApiFactory factory, ITestOutputHelper output)
    : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Service_order_should_return_200_or_201_and_walk_through_every_status_via_http()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Log("Setup", "Issue JWT token", tokenResponse.StatusCode);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Lifecycle Customer",
            email = "lifecycle@example.com",
            telephoneNumber = "+5511999990099",
            document = TestDocuments.ValidCpf(Interlocked.Increment(ref _documentCounter))
        });
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;
        Log("Setup", $"Create customer (id={customer.Id})", customerResponse.StatusCode);

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = "CVL1234",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        Assert.Equal(HttpStatusCode.Created, vehicleResponse.StatusCode);
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;
        Log("Setup", $"Create vehicle (id={vehicle.Id})", vehicleResponse.StatusCode);

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = "Lifecycle Mechanic" });
        Assert.Equal(HttpStatusCode.Created, mechanicResponse.StatusCode);
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;
        Log("Setup", $"Create mechanic (id={mechanic.Id})", mechanicResponse.StatusCode);

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = "Lifecycle Service",
            description = "Service used to exercise every status of the order",
            unitPrice = 100.00m,
            estimatedDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, workshopServiceResponse.StatusCode);
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;
        Log("Setup", $"Create workshop service (id={workshopService.Id})", workshopServiceResponse.StatusCode);

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order to validate the full lifecycle"
        });
        Assert.Equal(HttpStatusCode.Created, openResponse.StatusCode);
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        Log("0/6", "Open order", openResponse.StatusCode, serviceOrder.Status);

        var observedStatuses = new List<ServiceOrderStatus?> { serviceOrder.Status };

        var receivedResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            checkList = "Initial inspection completed"
        });
        Assert.Equal(HttpStatusCode.OK, receivedResponse.StatusCode);
        var receivedStatus = (await receivedResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(receivedStatus);
        Log("1/6", "Set checklist (expected: Received)", receivedResponse.StatusCode, receivedStatus);

        var inDiagnosisResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            mechanicId = mechanic.Id
        });
        Assert.Equal(HttpStatusCode.OK, inDiagnosisResponse.StatusCode);
        var inDiagnosisStatus = (await inDiagnosisResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(inDiagnosisStatus);
        Log("2/6", "Assign mechanic (expected: InDiagnosis)", inDiagnosisResponse.StatusCode, inDiagnosisStatus);

        var awaitingApprovalResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        });
        Assert.Equal(HttpStatusCode.OK, awaitingApprovalResponse.StatusCode);
        var awaitingApprovalStatus = (await awaitingApprovalResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(awaitingApprovalStatus);
        Log("3/6", "Attach workshop service (expected: AwaitingApproval)", awaitingApprovalResponse.StatusCode, awaitingApprovalStatus);

        var approveResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approveStatus = (await approveResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(approveStatus);
        Log("4/6", "Client approves (expected: InExecution)", approveResponse.StatusCode, approveStatus);

        var finalizeResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/finalize", content: null);
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var finalizeStatus = (await finalizeResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(finalizeStatus);
        Log("5/6", "Finalize (expected: Finalized)", finalizeResponse.StatusCode, finalizeStatus);

        var deliverResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/deliver", content: null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var deliverStatus = (await deliverResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(deliverStatus);
        Log("6/6", "Deliver (expected: Delivered)", deliverResponse.StatusCode, deliverStatus);

        Assert.Equal(
            new ServiceOrderStatus?[]
            {
                null,
                ServiceOrderStatus.Received,
                ServiceOrderStatus.InDiagnosis,
                ServiceOrderStatus.AwaitingApproval,
                ServiceOrderStatus.InExecution,
                ServiceOrderStatus.Finalized,
                ServiceOrderStatus.Delivered,
            },
            observedStatuses);

        output.WriteLine("");
        output.WriteLine("Summary: all 6 transitions happened in the expected order.");
    }

    // =====================================================================================
    // Group A - one test per status: confirms each status is reached by the right action.
    // =====================================================================================

    [Fact]
    public async Task Status_should_be_null_when_order_is_just_opened()
    {
        var ctx = await OpenNewOrderAsync();
        Log("null", "Order just opened", ctx.Order.Status);

        Assert.Null(ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_Received_after_checklist_is_set()
    {
        var ctx = await ReachReceivedAsync();
        Log("1 - Received", "Checklist informed", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.Received, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_InDiagnosis_after_mechanic_is_assigned()
    {
        var ctx = await ReachInDiagnosisAsync();
        Log("2 - InDiagnosis", "Mechanic assigned", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.InDiagnosis, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_AwaitingApproval_after_workshop_service_is_attached()
    {
        var ctx = await ReachAwaitingApprovalAsync();
        Log("3 - AwaitingApproval", "Workshop service attached", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_InExecution_after_client_approves()
    {
        var ctx = await ReachInExecutionAsync();
        Log("4 - InExecution", "Client approved", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.InExecution, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_Finalized_after_finalize()
    {
        var ctx = await ReachFinalizedAsync();
        Log("5 - Finalized", "Execution finalized", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.Finalized, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_Delivered_after_deliver()
    {
        var ctx = await ReachDeliveredAsync();
        Log("6 - Delivered", "Order delivered", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.Delivered, ctx.Order.Status);
    }

    [Fact]
    public async Task Status_should_be_Rejected_after_client_cancels()
    {
        var ctx = await ReachRejectedAsync();
        Log("7 - Rejected", "Client rejected (cancellation)", ctx.Order.Status);

        Assert.Equal(ServiceOrderStatus.Rejected, ctx.Order.Status);
    }

    // =====================================================================================
    // Group B - attempts to skip statuses: the system must block (400) every one of them.
    // =====================================================================================

    [Fact]
    public async Task Approve_should_be_blocked_when_order_was_just_opened()
    {
        var ctx = await OpenNewOrderAsync();

        var response = await ApproveAsync(ctx.Order.Id);
        Log("Skip straight to InExecution from null", "POST /approve", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approve_should_be_blocked_while_status_is_Received()
    {
        var ctx = await ReachReceivedAsync();

        var response = await ApproveAsync(ctx.Order.Id);
        Log("Skip InDiagnosis/AwaitingApproval, approve straight from Received", "POST /approve", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approve_should_be_blocked_while_status_is_InDiagnosis()
    {
        var ctx = await ReachInDiagnosisAsync();

        var response = await ApproveAsync(ctx.Order.Id);
        Log("Skip AwaitingApproval, approve straight from InDiagnosis", "POST /approve", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Finalize_should_be_blocked_when_order_was_just_opened()
    {
        var ctx = await OpenNewOrderAsync();

        var response = await FinalizeAsync(ctx.Order.Id);
        Log("Finalize a freshly opened order (null)", "POST /finalize", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Finalize_should_be_blocked_while_status_is_AwaitingApproval()
    {
        var ctx = await ReachAwaitingApprovalAsync();

        var response = await FinalizeAsync(ctx.Order.Id);
        Log("Skip InExecution, finalize straight from AwaitingApproval", "POST /finalize", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deliver_should_be_blocked_while_status_is_InExecution()
    {
        var ctx = await ReachInExecutionAsync();

        var response = await DeliverAsync(ctx.Order.Id);
        Log("Skip Finalized, deliver straight from InExecution", "POST /deliver", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_should_be_blocked_while_status_is_InDiagnosis()
    {
        var ctx = await ReachInDiagnosisAsync();

        var response = await CancelAsync(ctx.Order.Id);
        Log("Cancel before reaching AwaitingApproval (InDiagnosis)", "POST /cancel", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_should_be_blocked_while_status_is_InExecution()
    {
        var ctx = await ReachInExecutionAsync();

        var response = await CancelAsync(ctx.Order.Id);
        Log("Cancel after already approved (InExecution) - can only cancel while AwaitingApproval", "POST /cancel", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================================
    // Group C - terminal states: once Delivered or Rejected, no action should work anymore.
    // =====================================================================================

    [Fact]
    public async Task All_transitions_should_be_blocked_after_order_is_Delivered()
    {
        var ctx = await ReachDeliveredAsync();

        var approveResponse = await ApproveAsync(ctx.Order.Id);
        var cancelResponse = await CancelAsync(ctx.Order.Id);
        var finalizeResponse = await FinalizeAsync(ctx.Order.Id);
        var deliverResponse = await DeliverAsync(ctx.Order.Id);
        Log("Delivered -> approve", "POST /approve", approveResponse.StatusCode);
        Log("Delivered -> cancel", "POST /cancel", cancelResponse.StatusCode);
        Log("Delivered -> finalize", "POST /finalize", finalizeResponse.StatusCode);
        Log("Delivered -> deliver (again)", "POST /deliver", deliverResponse.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, finalizeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deliverResponse.StatusCode);
    }

    [Fact]
    public async Task All_transitions_should_be_blocked_after_order_is_Rejected()
    {
        var ctx = await ReachRejectedAsync();

        var approveResponse = await ApproveAsync(ctx.Order.Id);
        var cancelResponse = await CancelAsync(ctx.Order.Id);
        var finalizeResponse = await FinalizeAsync(ctx.Order.Id);
        var deliverResponse = await DeliverAsync(ctx.Order.Id);
        Log("Rejected -> approve", "POST /approve", approveResponse.StatusCode);
        Log("Rejected -> cancel (again)", "POST /cancel", cancelResponse.StatusCode);
        Log("Rejected -> finalize", "POST /finalize", finalizeResponse.StatusCode);
        Log("Rejected -> deliver", "POST /deliver", deliverResponse.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, finalizeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deliverResponse.StatusCode);
    }

    // =====================================================================================
    // Group D - attempts to "break" the flow via Update (change mechanic, attach new items
    // outside the allowed stage). Must be blocked (400), without changing the status.
    // =====================================================================================

    [Fact]
    public async Task Update_should_block_changing_mechanic_while_InDiagnosis()
    {
        var ctx = await ReachInDiagnosisAsync();

        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            mechanicId = Guid.NewGuid()
        });
        Log("Change mechanic while InDiagnosis", "PUT /service-orders", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_block_changing_mechanic_while_AwaitingApproval()
    {
        var ctx = await ReachAwaitingApprovalAsync();

        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            mechanicId = Guid.NewGuid()
        });
        Log("Change mechanic while AwaitingApproval", "PUT /service-orders", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_block_changing_mechanic_while_InExecution()
    {
        var ctx = await ReachInExecutionAsync();

        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            mechanicId = Guid.NewGuid()
        });
        Log("Change mechanic while InExecution", "PUT /service-orders", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_block_attaching_workshop_service_while_status_is_Received()
    {
        var ctx = await ReachReceivedAsync();

        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            workshopServiceIds = new[] { ctx.WorkshopServiceId }
        });
        Log("Attach workshop service before a mechanic is assigned (still Received)", "PUT /service-orders", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_should_block_adding_new_part_while_InExecution()
    {
        var ctx = await ReachInExecutionAsync();

        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            parts = new[] { new { partId = Guid.NewGuid(), quantity = 1 } }
        });
        Log("Attach a new part after the order is already InExecution", "PUT /service-orders", response.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================================
    // Support infrastructure: opens a new order and advances it to the requested stage,
    // reusing each step (every "ReachX" builds on top of the previous stage).
    // =====================================================================================

    private sealed record OrderContext(Guid MechanicId, Guid WorkshopServiceId, ServiceOrderDetailResponse Order);

    private async Task<OrderContext> OpenNewOrderAsync()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "State Machine Customer",
            email = $"statemachine.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"MEQ{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"State Machine Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"State Machine Service {sequence}",
            description = "Service used to test the order's state machine",
            unitPrice = 100m,
            estimatedDurationMinutes = 30
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order to test the state machine"
        });
        openResponse.EnsureSuccessStatusCode();
        var order = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        return new OrderContext(mechanic.Id, workshopService.Id, order);
    }

    private async Task<OrderContext> ReachReceivedAsync()
    {
        var ctx = await OpenNewOrderAsync();
        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            checkList = "Initial inspection completed"
        });
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachInDiagnosisAsync()
    {
        var ctx = await ReachReceivedAsync();
        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            mechanicId = ctx.MechanicId
        });
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachAwaitingApprovalAsync()
    {
        var ctx = await ReachInDiagnosisAsync();
        var response = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = ctx.Order.Id,
            workshopServiceIds = new[] { ctx.WorkshopServiceId }
        });
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachInExecutionAsync()
    {
        var ctx = await ReachAwaitingApprovalAsync();
        var response = await ApproveAsync(ctx.Order.Id);
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachFinalizedAsync()
    {
        var ctx = await ReachInExecutionAsync();
        var response = await FinalizeAsync(ctx.Order.Id);
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachDeliveredAsync()
    {
        var ctx = await ReachFinalizedAsync();
        var response = await DeliverAsync(ctx.Order.Id);
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private async Task<OrderContext> ReachRejectedAsync()
    {
        var ctx = await ReachAwaitingApprovalAsync();
        var response = await CancelAsync(ctx.Order.Id);
        response.EnsureSuccessStatusCode();
        var order = (await response.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        return ctx with { Order = order };
    }

    private Task<HttpResponseMessage> ApproveAsync(Guid serviceOrderId) =>
        _client.PostAsync($"/api/v1/service-orders/{serviceOrderId}/approve", content: null);

    private Task<HttpResponseMessage> CancelAsync(Guid serviceOrderId) =>
        _client.PostAsync($"/api/v1/service-orders/{serviceOrderId}/cancel", content: null);

    private Task<HttpResponseMessage> FinalizeAsync(Guid serviceOrderId) =>
        _client.PostAsync($"/api/v1/service-orders/{serviceOrderId}/finalize", content: null);

    private Task<HttpResponseMessage> DeliverAsync(Guid serviceOrderId) =>
        _client.PostAsync($"/api/v1/service-orders/{serviceOrderId}/deliver", content: null);

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

    private void Log(string step, string action, HttpStatusCode statusCode, ServiceOrderStatus? osStatus = null)
    {
        var osStatusText = osStatus is null ? "" : $" | order status = {osStatus}";
        output.WriteLine($"[{step}] {action} -> {(int)statusCode} {statusCode}{osStatusText}");
    }

    private void Log(string step, string action, ServiceOrderStatus? osStatus) =>
        output.WriteLine($"[{step}] {action} -> order status = {(osStatus is null ? "null" : osStatus.ToString())}");
}
