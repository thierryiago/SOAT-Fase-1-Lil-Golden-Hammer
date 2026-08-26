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
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Service_order_should_return_200_or_201_and_walk_through_every_status_via_http()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Log("Setup", "Emitir token JWT", tokenResponse.StatusCode);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Cliente Ciclo de Vida",
            email = "ciclo.vida@example.com",
            telephoneNumber = "+5511999990099",
            document = "12345678901"
        });
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>())!;
        Log("Setup", $"Criar cliente (id={customer.Id})", customerResponse.StatusCode);

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
        Log("Setup", $"Criar veiculo (id={vehicle.Id})", vehicleResponse.StatusCode);

        var mechanicResponse = await _client.PostAsJsonAsync("/api/v1/mechanics", new { name = "Mecanico Ciclo de Vida" });
        Assert.Equal(HttpStatusCode.Created, mechanicResponse.StatusCode);
        var mechanic = (await mechanicResponse.Content.ReadFromJsonAsync<MechanicResponse>())!;
        Log("Setup", $"Criar mecanico (id={mechanic.Id})", mechanicResponse.StatusCode);

        var workshopServiceResponse = await _client.PostAsJsonAsync("/api/v1/workshop-services", new
        {
            name = "Servico Ciclo de Vida",
            description = "Servico usado para exercitar todos os status da OS",
            unitPrice = 100.00m,
            estimatedDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, workshopServiceResponse.StatusCode);
        var workshopService = (await workshopServiceResponse.Content.ReadFromJsonAsync<WorkshopServiceResponse>())!;
        Log("Setup", $"Criar servico de oficina (id={workshopService.Id})", workshopServiceResponse.StatusCode);

        var openResponse = await _client.PostAsJsonAsync("/api/v1/service-orders", new
        {
            customerId = customer.Id,
            vehicleId = vehicle.Id,
            description = "OS para validar o ciclo de vida completo"
        });
        Assert.Equal(HttpStatusCode.Created, openResponse.StatusCode);
        var serviceOrder = (await openResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!;
        Log("0/6", "Abrir OS", openResponse.StatusCode, serviceOrder.Status);

        var observedStatuses = new List<ServiceOrderStatus?> { serviceOrder.Status };

        var receivedResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            checkList = "Inspecao inicial concluida"
        });
        Assert.Equal(HttpStatusCode.OK, receivedResponse.StatusCode);
        var receivedStatus = (await receivedResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(receivedStatus);
        Log("1/6", "Definir checklist (esperado: Received)", receivedResponse.StatusCode, receivedStatus);

        var inDiagnosisResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            mechanicId = mechanic.Id
        });
        Assert.Equal(HttpStatusCode.OK, inDiagnosisResponse.StatusCode);
        var inDiagnosisStatus = (await inDiagnosisResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(inDiagnosisStatus);
        Log("2/6", "Atribuir mecanico (esperado: InDiagnosis)", inDiagnosisResponse.StatusCode, inDiagnosisStatus);

        var awaitingApprovalResponse = await _client.PutAsJsonAsync("/api/v1/service-orders", new
        {
            serviceOrderId = serviceOrder.Id,
            workshopServiceIds = new[] { workshopService.Id }
        });
        Assert.Equal(HttpStatusCode.OK, awaitingApprovalResponse.StatusCode);
        var awaitingApprovalStatus = (await awaitingApprovalResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(awaitingApprovalStatus);
        Log("3/6", "Anexar servico de oficina (esperado: AwaitingApproval)", awaitingApprovalResponse.StatusCode, awaitingApprovalStatus);

        var approveResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approveStatus = (await approveResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(approveStatus);
        Log("4/6", "Cliente aprova (esperado: InExecution)", approveResponse.StatusCode, approveStatus);

        var finalizeResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/finalize", content: null);
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var finalizeStatus = (await finalizeResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(finalizeStatus);
        Log("5/6", "Finalizar (esperado: Finalized)", finalizeResponse.StatusCode, finalizeStatus);

        var deliverResponse = await _client.PostAsync($"/api/v1/service-orders/{serviceOrder.Id}/deliver", content: null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var deliverStatus = (await deliverResponse.Content.ReadFromJsonAsync<ServiceOrderDetailResponse>())!.Status;
        observedStatuses.Add(deliverStatus);
        Log("6/6", "Entregar (esperado: Delivered)", deliverResponse.StatusCode, deliverStatus);

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
        output.WriteLine("Resumo: todas as 6 transicoes ocorreram na ordem esperada.");
    }

    private void Log(string step, string action, HttpStatusCode statusCode, ServiceOrderStatus? osStatus = null)
    {
        var osStatusText = osStatus is null ? "" : $" | status da OS = {osStatus}";
        output.WriteLine($"[{step}] {action} -> {(int)statusCode} {statusCode}{osStatusText}");
    }
}
