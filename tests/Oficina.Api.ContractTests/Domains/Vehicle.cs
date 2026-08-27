using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class VehicleTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("ABC1234", "formato antigo sem hifen")]
    [InlineData("XYZ-9876", "formato antigo com hifen")]
    [InlineData("ABC1D23", "formato Mercosul")]
    public async Task Create_should_accept_valid_plate(string plate, string description)
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate,
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log($"Placa valida - {description} (\"{plate}\")", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("AB1234", "letras insuficientes (2 letras + 4 digitos)")]
    [InlineData("ABCD1234", "letras em excesso (4 letras + 4 digitos)")]
    [InlineData("ABC123", "digitos insuficientes no formato antigo (3 letras + 3 digitos)")]
    [InlineData("1234ABC", "comeca com numeros")]
    [InlineData("ABC1DD3", "duas letras na posicao de digito (formato Mercosul malformado)")]
    [InlineData("", "placa vazia")]
    public async Task Create_should_reject_invalid_plate(string plate, string description)
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate,
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log($"Placa invalida - {description} (\"{plate}\")", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_same_plate_typed_with_different_casing_as_duplicate()
    {
        var customerA = await CreateCustomerAsync();
        var customerB = await CreateCustomerAsync();

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customerA.Id,
            plate = "DUP5678",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customerB.Id,
            plate = "dup5678",
            brand = "Toyota",
            model = "Corolla",
            year = 2021,
            category = 1
        });
        Log("Mesma placa fisica cadastrada de novo em minusculo (esperado: rejeitado como duplicado)", secondResponse);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Cliente Teste de Placa",
            email = $"placa.{sequence}@example.com",
            telephoneNumber = "+5511999990000",
            document = sequence.ToString().PadLeft(11, '0')
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
    }

    private async Task AuthenticateAsync()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void Log(string scenario, HttpResponseMessage response) =>
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
}
