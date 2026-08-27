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

// BudgetsController so expoe GET (lista) e GET /{id} - nao ha endpoint HTTP para abrir um
// orcamento (BudgetService.OpenFromServiceOrderAsync existe na camada de aplicacao mas nunca foi
// ligado a uma rota). Por isso, ao contrario dos outros arquivos em Domains/, aqui preparamos o
// cenario inserindo direto no AppDbContext do host de teste, e testamos a leitura via HTTP.
// BudgetsController tambem nao tem [Authorize] hoje - as chamadas abaixo propositalmente nao
// enviam token, refletindo o comportamento real do controller.
public sealed class BudgetTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_should_return_registered_budgets_without_authentication()
    {
        var (budgetId, _) = await SeedBudgetAsync();

        var response = await _client.GetAsync("/api/v1/budgets?page=1&pageSize=20");
        Log("Listar orcamentos (sem token de autenticacao)", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var budgets = (await response.Content.ReadFromJsonAsync<PagedResponse<BudgetResponse>>())!;

        Assert.Contains(budgets.Items, budget => budget.Id == budgetId);
    }

    [Fact]
    public async Task GetById_should_return_budget_with_calculated_total_value()
    {
        var (budgetId, expectedTotal) = await SeedBudgetAsync();

        var response = await _client.GetAsync($"/api/v1/budgets/{budgetId}");
        Log($"Buscar orcamento por id (esperado: totalValue={expectedTotal})", response);
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
        Log("Buscar orcamento inexistente", response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Service_order_should_open_a_budget_when_it_reaches_awaiting_approval()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Cliente Orcamento Automatico",
            email = $"orcamento.auto.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = sequence.ToString().PadLeft(11, '0')
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

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = $"Mecanico Orcamento {sequence}" });
        mechanicResponse.EnsureSuccessStatusCode();
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;

        var serviceAResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Servico Orcamento A {sequence}",
            description = "Servico de 400 reais",
            unitPrice = 400m,
            estimatedDurationMinutes = 60
        });
        serviceAResponse.EnsureSuccessStatusCode();
        var serviceA = (await serviceAResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var serviceBResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = $"Servico Orcamento B {sequence}",
            description = "Outro servico de 400 reais",
            unitPrice = 400m,
            estimatedDurationMinutes = 60
        });
        serviceBResponse.EnsureSuccessStatusCode();
        var serviceB = (await serviceBResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "OS para validar abertura automatica de orcamento"
        });
        openResponse.EnsureSuccessStatusCode();
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;

        var checklistResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            checkList = "Inspecao inicial concluida"
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
        Log($"OS avancou para {updatedOrder.Status} apos anexar 2 servicos de R$400", awaitingApprovalResponse);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, updatedOrder.Status);

        var budgetsResponse = await _client.GetAsync("/api/v1/budgets?page=1&pageSize=100");
        budgetsResponse.EnsureSuccessStatusCode();
        var budgets = (await budgetsResponse.Content.ReadFromJsonAsync<PagedResponse<BudgetResponse>>())!;
        var budgetForThisOrder = budgets.Items.SingleOrDefault(budget => budget.ServiceOrderId == serviceOrder.Id);
        Log(
            budgetForThisOrder is null
                ? "Nenhum orcamento encontrado para a OS (esperado: um orcamento de R$800)"
                : $"Orcamento encontrado para a OS: TotalValue={budgetForThisOrder.TotalValue}",
            budgetsResponse);

        // REGRA DE NEGOCIO ESPERADA (2026-08-26, ainda NAO implementada): quando a OS atinge
        // AwaitingApproval, o sistema deveria abrir automaticamente um Budget vinculado a ela,
        // com TotalValue = soma dos servicos de oficina anexados (aqui, 400 + 400 = 800).
        // Hoje isso nao acontece em lugar nenhum do codigo: ServiceOrderService nao conhece
        // BudgetService/IBudgetRepository, e BudgetService.OpenFromServiceOrderAsync so pode
        // ser chamado manualmente (nem isso - nao ha endpoint HTTP para ele). Este teste documenta
        // o comportamento correto esperado e falha propositalmente ate a regra ser implementada.
        Assert.NotNull(budgetForThisOrder);
        Assert.Equal(800m, budgetForThisOrder!.TotalValue);
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
            "Cliente Orcamento", $"orcamento.{sequence}@example.com", "+5511999990000",
            sequence.ToString().PadLeft(11, '0'));
        db.Customers.Add(customer);

        var part = Part.Create("Peca Orcamento", $"BUD-{Guid.NewGuid():N}", 50m, EnumPartKind.Part);
        db.Parts.Add(part);

        var workshopService = WorkshopService.Create($"Servico Orcamento {sequence}", "Descricao", 200m, 30);
        db.WorkshopServices.Add(workshopService);

        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "OS para orcamento");
        db.ServiceOrders.Add(serviceOrder);

        await db.SaveChangesAsync();

        var budgetId = Guid.NewGuid();
        const int partQuantity = 2;
        var budgetPart = BudgetParts.Create(budgetId, part.Id, partQuantity);
        budgetPart.Part = part;
        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopService.Id);
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
