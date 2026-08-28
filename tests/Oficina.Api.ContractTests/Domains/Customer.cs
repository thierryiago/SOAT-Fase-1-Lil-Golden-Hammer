using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
            name = "Valid CPF Customer",
            email = "cpf.valid@example.com",
            telephoneNumber = "+5511999990001",
            document = "086.043.100-29"
        });
        Log("Valid CPF (086.043.100-29 - check digits match: 2 and 9)", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_cpf_with_invalid_check_digit()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Invalid CPF Customer",
            email = "cpf.invalid@example.com",
            telephoneNumber = "+5511999990002",
            document = "123.456.789-01"
        });
        Log("CPF with invalid check digit (123.456.789-01 - second digit should be 9, not 1)", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_accept_customer_with_valid_cnpj()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Valid CNPJ Company",
            email = "cnpj.valid@example.com",
            telephoneNumber = "+5511999990003",
            document = "11.222.333/0001-81"
        });
        Log("Valid CNPJ (11.222.333/0001-81 - check digits match: 8 and 1)", response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_reject_cnpj_with_invalid_check_digit()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Invalid CNPJ Company",
            email = "cnpj.invalid@example.com",
            telephoneNumber = "+5511999990004",
            document = "11.222.333/0001-00"
        });
        Log("CNPJ with invalid check digit (11.222.333/0001-00 - should be 81, not 00)", response);

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
