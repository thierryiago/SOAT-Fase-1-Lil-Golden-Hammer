using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class CustomerTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private static int _documentCounter;

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

    // Item 7 of docs/analise-gaps-e-cenarios-faltantes.md: a document containing letters must be
    // rejected by model validation (DocumentValidatorAttribute) with 400, before the request even
    // reaches CustomerService/the domain.
    [Fact]
    public async Task Create_should_reject_document_containing_letters()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Document With Letters",
            email = "letters.document@example.com",
            telephoneNumber = "+5511999990005",
            document = "123.ABC.789-01"
        });
        Log("Document containing letters (\"123.ABC.789-01\") - expected: 400 model validation error", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Item 9 of docs/analise-gaps-e-cenarios-faltantes.md: the same physical CPF, typed with a
    // different mask/formatting, must still be detected as a duplicate (409) - not accepted as a
    // "different" document because the raw string differs.
    [Fact]
    public async Task Create_should_reject_same_cpf_typed_with_different_formatting_as_duplicate()
    {
        await AuthenticateAsync();

        var sequence = Interlocked.Increment(ref _documentCounter);
        var document = TestDocuments.ValidCpf(sequence);
        var formattedDocument = FormatAsCpf(document);

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Unformatted Document Customer",
            email = $"unformatted.{sequence}@example.com",
            telephoneNumber = "+5511999990006",
            document
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            name = "Formatted Document Customer",
            email = $"formatted.{sequence}@example.com",
            telephoneNumber = "+5511999990007",
            document = formattedDocument
        });
        Log($"Same CPF, first unformatted (\"{document}\") then formatted (\"{formattedDocument}\") - expected: duplicate (409)", secondResponse);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    // Item 19 of docs/analise-gaps-e-cenarios-faltantes.md: invalid pagination parameters must
    // surface as 400 through the real HTTP pipeline (Pagination.Create already throws
    // ArgumentOutOfRangeException, which Program.cs's exception handler maps to 400).
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_should_reject_invalid_pagination_parameters(int page, int pageSize)
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/v1/customers?page={page}&pageSize={pageSize}");
        Log($"List with page={page}, pageSize={pageSize} (expected: 400)", response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string FormatAsCpf(string digitsOnly) =>
        $"{digitsOnly[..3]}.{digitsOnly[3..6]}.{digitsOnly[6..9]}-{digitsOnly[9..]}";

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

    private void Log(string scenario, HttpResponseMessage response)
    {
        output.WriteLine($"[{scenario}] -> {(int)response.StatusCode} {response.StatusCode}");
    }
}
