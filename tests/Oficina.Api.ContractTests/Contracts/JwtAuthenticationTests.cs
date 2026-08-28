using Microsoft.IdentityModel.Tokens;
using Oficina.Api.ContractTests.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class JwtAuthenticationTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Fact]
    public async Task Administrative_route_without_token_should_return_unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administrative_route_with_valid_token_should_be_reached()
    {
        var token = CreateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/customers");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True((int)response.StatusCode < 500);
    }

    [Theory]
    [InlineData("wrong-issuer", OficinaApiFactory.JwtAudience, OficinaApiFactory.JwtSigningKey, -60)]
    [InlineData(OficinaApiFactory.JwtIssuer, "wrong-audience", OficinaApiFactory.JwtSigningKey, -60)]
    [InlineData(OficinaApiFactory.JwtIssuer, OficinaApiFactory.JwtAudience, "different-tests-only-signing-key-at-least-32-bytes", -60)]
    [InlineData(OficinaApiFactory.JwtIssuer, OficinaApiFactory.JwtAudience, OficinaApiFactory.JwtSigningKey, -3600)]
    public async Task Invalid_token_should_return_unauthorized(
        string issuer,
        string audience,
        string signingKey,
        int validFromOffsetSeconds)
    {
        var token = CreateToken(issuer, audience, signingKey, validFromOffsetSeconds);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_token_should_return_unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/customers")).StatusCode);
    }

    [Fact]
    public async Task Infrastructure_and_token_routes_should_remain_anonymous()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync("/api/v1/auth/token", null)).StatusCode);
    }

    private static string CreateToken(
        string issuer = OficinaApiFactory.JwtIssuer,
        string audience = OficinaApiFactory.JwtAudience,
        string signingKey = OficinaApiFactory.JwtSigningKey,
        int validFromOffsetSeconds = -60)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim(JwtRegisteredClaimNames.Sub, "contract-test-admin")],
            notBefore: now.AddSeconds(validFromOffsetSeconds),
            expires: now.AddSeconds(validFromOffsetSeconds + 900),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
