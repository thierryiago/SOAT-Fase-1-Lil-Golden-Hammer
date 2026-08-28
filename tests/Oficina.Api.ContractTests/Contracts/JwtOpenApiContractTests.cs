using Oficina.Api.ContractTests.Infrastructure;
using System.Text.Json;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class JwtOpenApiContractTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_should_describe_bearer_security_only_for_protected_operations()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = document.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/v1/customers").GetProperty("get").GetProperty("security").GetArrayLength() > 0);
        Assert.False(paths.GetProperty("/api/v1/auth/token").GetProperty("post").TryGetProperty("security", out var tokenSecurity)
            && tokenSecurity.GetArrayLength() > 0);
    }
}
