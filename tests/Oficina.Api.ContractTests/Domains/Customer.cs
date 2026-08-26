using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class CustomerTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_should_accept_customer_with_valid_cpf()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Cliente CPF Valido",
            email = "cpf.valido@example.com",
            telephoneNumber = "+5511999990001",
            document = "086.043.100-29"
        });
        Log("CPF valido (086.043.100-29 - digitos verificadores conferem: 2 e 9)", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_cpf_with_invalid_check_digit()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Cliente CPF Invalido",
            email = "cpf.invalido@example.com",
            telephoneNumber = "+5511999990002",
            document = "123.456.789-01"
        });
        Log("CPF com digito verificador invalido (123.456.789-01 - segundo digito deveria ser 9, nao 1)", response);

        // BUG CONHECIDO (2026-08-26): Customer.IsValidDocument (src/Oficina.Domain/Customers/Customer.cs)
        // nao calcula o digito verificador real (mod-11) do CPF/CNPJ - so rejeita documentos com todos
        // os digitos repetidos (ex.: 000.000.000-00). Um CPF com 11 digitos nao repetidos, mesmo com
        // digito verificador matematicamente errado, e aceito hoje. Este teste documenta o comportamento
        // CORRETO esperado e falha propositalmente ate a validacao real ser implementada.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_accept_customer_with_valid_cnpj()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Empresa CNPJ Valido",
            email = "cnpj.valido@example.com",
            telephoneNumber = "+5511999990003",
            document = "11.222.333/0001-81"
        });
        Log("CNPJ valido (11.222.333/0001-81 - digitos verificadores conferem: 8 e 1)", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_cnpj_with_invalid_check_digit()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Empresa CNPJ Invalido",
            email = "cnpj.invalido@example.com",
            telephoneNumber = "+5511999990004",
            document = "11.222.333/0001-00"
        });
        Log("CNPJ com digito verificador invalido (11.222.333/0001-00 - deveria ser 81, nao 00)", response);

        // BUG CONHECIDO (2026-08-26): mesma causa raiz do teste de CPF acima.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        var tokenResponse = await _client.PostAsync("/api/v1/auth/token", content: null);
        var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void Log(string scenario, HttpResponseMessage response)
    {
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
    }
}
