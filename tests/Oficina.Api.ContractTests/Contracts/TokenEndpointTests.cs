using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class TokenEndpointTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Token_endpoint_should_issue_a_basic_jwt_without_credentials()
    {
        var response = await _client.PostAsync("/api/v1/auth/token", content: null);
        var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tokenResponse);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.True(tokenResponse.ExpiresAt > DateTimeOffset.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.AccessToken);
        Assert.Equal(OficinaApiFactory.JwtIssuer, token.Issuer);
        Assert.Contains(OficinaApiFactory.JwtAudience, token.Audiences);
        Assert.Equal("oficina-admin", token.Subject);
        Assert.DoesNotContain(token.Claims, claim => claim.Type is "role" or "roles");
    }
}
