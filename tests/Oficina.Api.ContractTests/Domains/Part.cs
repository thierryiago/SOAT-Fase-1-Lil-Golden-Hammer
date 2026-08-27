using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.Stocks;
using Xunit.Abstractions;

namespace Oficina.Api.ContractTests.Domains;

public sealed class PartTests(OficinaApiFactory factory, ITestOutputHelper output) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_should_register_part_and_a_zeroed_stock()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = "Oil filter",
            code = "PART-TEST-001",
            unitPrice = 45.90m,
            kind = 1
        });
        Log("Create part", createResponse);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var part = (await createResponse.Content.ReadFromJsonAsync<PartResponse>())!;

        var stocksResponse = await _client.GetAsync("/api/v1/stocks?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, stocksResponse.StatusCode);
        var stocks = (await stocksResponse.Content.ReadFromJsonAsync<PagedResponse<StockResponse>>())!;
        var stock = stocks.Items.SingleOrDefault(s => s.PartId == part.Id);
        Log($"Stock automatically created for the part (id={stock?.Id}, quantity={stock?.Quantity})", stocksResponse);

        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
    }

    [Fact]
    public async Task Create_should_reject_duplicate_code()
    {
        await AuthenticateAsync();

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = "Air filter",
            code = "PART-TEST-DUP",
            unitPrice = 30m,
            kind = 1
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = "Some other filter",
            code = "PART-TEST-DUP",
            unitPrice = 99m,
            kind = 1
        });
        Log("Create part with duplicate code", duplicateResponse);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_should_change_part_data()
    {
        await AuthenticateAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/parts", new
        {
            name = "Spark plug",
            code = "PART-TEST-UPD",
            unitPrice = 20m,
            kind = 1
        });
        var part = (await createResponse.Content.ReadFromJsonAsync<PartResponse>())!;

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/parts/{part.Id}", new
        {
            name = "Iridium spark plug",
            code = "PART-TEST-UPD",
            unitPrice = 35m,
            kind = 1
        });
        Log("Update part", updateResponse);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<PartResponse>())!;

        Assert.Equal("Iridium spark plug", updated.Name);
        Assert.Equal(35m, updated.UnitPrice);
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
