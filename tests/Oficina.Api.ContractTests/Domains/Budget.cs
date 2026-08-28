using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Budgets;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

// BudgetsController only exposes GET (list) and GET /{id} - there is no HTTP endpoint to open a
// budget. Unlike the other files in Domains/, the scenario here is prepared by inserting directly
// into the test host's AppDbContext, and reading is tested via HTTP.
// BudgetsController also has no [Authorize] today - the calls below deliberately send no token,
// reflecting the controller's actual behavior.
public sealed class BudgetTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_should_return_registered_budgets_without_authentication()
    {
        var (budgetId, _) = await SeedBudgetAsync();

        var response = await _client.GetAsync("/api/v1/budgets?page=1&pageSize=20");
        Log("List budgets (no authentication token)", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var budgets = (await response.Content.ReadFromJsonAsync<PagedResponse<BudgetResponse>>())!;

        Assert.Contains(budgets.Items, budget => budget.Id == budgetId);
    }

    [Fact]
    public async Task GetById_should_return_budget_with_calculated_total_value()
    {
        var (budgetId, expectedTotal) = await SeedBudgetAsync();

        var response = await _client.GetAsync($"/api/v1/budgets/{budgetId}");
        Log($"Get budget by id (expected: totalValue={expectedTotal})", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var budget = (await response.Content.ReadFromJsonAsync<BudgetResponse>())!;

        Assert.Equal(expectedTotal, budget.TotalValue);
        Assert.Single(budget.Parts);
        Assert.Single(budget.WorkshopServices);
    }

    [Fact]
    public async Task GetById_should_return_not_found_for_unknown_budget()
    {
        var response = await _client.GetAsync($"/api/v1/budgets/{Guid.NewGuid()}");
        Log("Get unknown budget", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Service_order_should_open_a_budget_when_it_reaches_awaiting_approval()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Auto Budget Customer",
            email = $"auto.budget.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"BUD{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"Budget Test Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var serviceAResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Budget Test Service A {sequence}",
            description = "A $400 service",
            unitPrice = 400m,
            estimatedDurationMinutes = 60
        });
        serviceAResponse.EnsureSuccessStatusCode();
        var serviceA = (await serviceAResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var serviceBResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Budget Test Service B {sequence}",
            description = "Another $400 service",
            unitPrice = 400m,
            estimatedDurationMinutes = 60
        });
        serviceBResponse.EnsureSuccessStatusCode();
        var serviceB = (await serviceBResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order to validate automatic budget creation"
        });
        openResponse.EnsureSuccessStatusCode();
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var checklistResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            checkList = "Initial inspection completed"
        });
        checklistResponse.EnsureSuccessStatusCode();

        var mechanicAssignResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            mechanicId = mechanic.Id
        });
        mechanicAssignResponse.EnsureSuccessStatusCode();

        var awaitingApprovalResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { serviceA.Id, serviceB.Id }
        });
        awaitingApprovalResponse.EnsureSuccessStatusCode();
        var updatedOrder = (await awaitingApprovalResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        Log($"Order advanced to {updatedOrder.Status} after attaching 2 services of $400", awaitingApprovalResponse);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, updatedOrder.Status);

        var budgetsResponse = await _client.GetAsync("/api/v1/budgets?page=1&pageSize=100");
        budgetsResponse.EnsureSuccessStatusCode();
        var budgets = (await budgetsResponse.Content.ReadFromJsonAsync<PagedResponse<BudgetResponse>>())!;
        var budgetForThisOrder = budgets.Items.SingleOrDefault(budget => budget.ServiceOrderId == serviceOrder.Id);
        Log(
            budgetForThisOrder is null
                ? "No budget found for the order (expected: a budget of $800)"
                : $"Budget found for the order: TotalValue={budgetForThisOrder.TotalValue}",
            budgetsResponse);

        Assert.NotNull(budgetForThisOrder);
        Assert.Equal(800m, budgetForThisOrder!.TotalValue);
    }

    // Item 18 of docs/analise-gaps-e-cenarios-faltantes.md: confirms the budget-awaiting-approval
    // e-mail is actually dispatched (not just that NotificationService is called correctly with
    // fakes, as already tested at the Application layer) when a real order reaches
    // AwaitingApproval via HTTP. OficinaApiFactory registers a FakeNotificationEmailSender in
    // place of the real SMTP sender, capturing every "sent" e-mail for inspection.
    [Fact]
    public async Task Service_order_should_actually_dispatch_the_budget_email_when_it_reaches_awaiting_approval()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerEmail = $"budget.email.{sequence}@example.com";
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Budget Email Customer",
            email = customerEmail,
            telephoneNumber = "+5511999990000",
            document = TestDocuments.ValidCpf(sequence)
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;

        var vehicleResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"MAI{sequence:0000}",
            brand = "Fiat",
            model = "Uno",
            year = 2020,
            category = 1
        });
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"Budget Email Mechanic {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Budget Email Service {sequence}",
            description = "A $500 service",
            unitPrice = 500m,
            estimatedDurationMinutes = 60
        });
        workshopServiceResponse.EnsureSuccessStatusCode();
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "Order to validate the real budget e-mail dispatch"
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

        var awaitingApprovalResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        });
        awaitingApprovalResponse.EnsureSuccessStatusCode();

        var sentEmail = OficinaApiFactory.FakeNotificationEmailSender.SentEmails
            .SingleOrDefault(email => email.Recipient == customerEmail);
        Log(
            sentEmail is null
                ? "No budget e-mail captured for this customer"
                : $"Budget e-mail captured: subject=\"{sentEmail.Subject}\"",
            awaitingApprovalResponse);

        Assert.NotNull(sentEmail);
        Assert.Contains("Budget Awaiting to Approval", sentEmail!.Subject);
        Assert.Contains("500.00", sentEmail.Body);
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

    private async Task<(Guid BudgetId, decimal ExpectedTotal)> SeedBudgetAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customer = Customer.Create(
            "Budget Test Customer", $"budget.{sequence}@example.com", "+5511999990000",
            TestDocuments.ValidCpf(sequence));
        db.Customers.Add(customer);

        var part = Part.Create("Budget Test Part", $"BUD-{Guid.NewGuid():N}", 50m, EnumPartKind.Part);
        db.Parts.Add(part);

        var workshopService = WorkshopService.Create($"Budget Test Service {sequence}", "Description", 200m, 30);
        db.WorkshopServices.Add(workshopService);

        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Order for budget test");
        db.ServiceOrders.Add(serviceOrder);

        await db.SaveChangesAsync();

        var budgetId = Guid.NewGuid();
        const int partQuantity = 2;
        var budgetPart = BudgetParts.Create(
            budgetId, part.Id, part.Name, part.UnitPrice, partQuantity);
        budgetPart.Part = part;
        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
        budgetWorkshopService.WorkshopService = workshopService;

        var budget = Budget.Open(
            budgetId, customer.Id, serviceOrder.Id, [budgetPart], [budgetWorkshopService]);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        var expectedTotal = (partQuantity * part.UnitPrice) + workshopService.UnitPrice;
        return (budget.Id, expectedTotal);
    }

    private void Log(string scenario, HttpResponseMessage response) =>
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
}
