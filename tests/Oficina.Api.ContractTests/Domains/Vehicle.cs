using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Customers;
using Oficina.Application.Vehicles;
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

    [Fact]
    public async Task Create_should_accept_lowercase_plate()
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = "low1234",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log("Placa minuscula (\"low1234\")", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("A#B$C%9^9&9*9", "simbolos especiais espalhados pela placa")]
    [InlineData("AB C.999.9", "pontuacao e espaco misturados")]
    public async Task Create_should_reject_plate_with_special_characters(string plate, string description)
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
        Log($"Placa com caracteres especiais - {description} (\"{plate}\")", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Item 25 of docs/analise-gaps-e-cenarios-faltantes.md - DELIBERATELY RED: EnumVehicleCategory
    // has no [EnumDataType]/JsonStringEnumConverter validation anywhere in the pipeline (verified
    // in src/Oficina.Application/Clientes/VehicleDtos.cs and Vehicle.Validate), so System.Text.Json
    // happily deserializes any integer into the enum and Vehicle.Create stores it as-is. The API
    // should reject an out-of-range category with 400; today it accepts it with 201. Documents a
    // real gap - do not add [EnumDataType] here, only the test.
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public async Task Create_should_reject_category_outside_the_enum_range(int category)
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            customerId = customer.Id,
            plate = $"CAT{Math.Abs(category):0000}",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category
        });
        Log($"Category outside EnumVehicleCategory's range (\"{category}\") - expected: 400", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Item 14 of docs/analise-gaps-e-cenarios-faltantes.md: POST /api/v1/vehicles/identify-customer-and-register.
    [Fact]
    public async Task IdentifyCustomerAndRegister_should_register_vehicle_for_existing_customer()
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles/identify-customer-and-register", new
        {
            document = customer.Document,
            plate = $"IDE{Interlocked.Increment(ref _documentCounter):0000}",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log("Identify customer by document and register a new vehicle", response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var registration = (await response.Content.ReadFromJsonAsync<CustomerVehicleRegistrationResponse>())!;
        Assert.Equal(customer.Id, registration.CustomerId);
        Assert.Equal(customer.Document, registration.Document);
    }

    [Fact]
    public async Task IdentifyCustomerAndRegister_should_fail_when_document_is_not_found()
    {
        await AuthenticateAsync();
        var sequence = Interlocked.Increment(ref _documentCounter);

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles/identify-customer-and-register", new
        {
            document = TestDocuments.ValidCpf(sequence + 800_000),
            plate = $"NFD{sequence:0000}",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log("Identify customer by a document that was never registered", response);

        // VehicleService throws KeyNotFoundException, which Program.cs's exception handler maps to 404.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IdentifyCustomerAndRegister_should_fail_when_customer_is_inactive()
    {
        var customer = await CreateCustomerAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles/identify-customer-and-register", new
        {
            document = customer.Document,
            plate = $"INA{Interlocked.Increment(ref _documentCounter):0000}",
            brand = "Honda",
            model = "Civic",
            year = 2022,
            category = 1
        });
        Log("Identify customer by document after the customer was soft-deleted (inactive)", response);

        // GetByDocumentAsync (see CustomerRepository) may or may not still return the inactive
        // customer; either way VehicleService checks IsActive and throws KeyNotFoundException,
        // mapped to 404 by Program.cs's exception handler.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            document = TestDocuments.ValidCpf(sequence)
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
