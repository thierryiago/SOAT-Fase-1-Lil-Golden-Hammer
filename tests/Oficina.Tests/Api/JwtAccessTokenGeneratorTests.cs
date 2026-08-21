using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Oficina.Api.Authentication;

namespace Oficina.Tests.Api;

public sealed class JwtAccessTokenGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_should_create_a_signed_short_lived_token_without_roles()
    {
        var options = ValidOptions();
        var generator = new JwtAccessTokenGenerator(
            Options.Create(options),
            new FixedTimeProvider(Now));

        var response = generator.Generate();
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(response.AccessToken, ValidationParameters(options), out var validated);
        var token = Assert.IsType<JwtSecurityToken>(validated);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(Now.AddMinutes(15), response.ExpiresAt);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Equal("oficina-admin", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.NotNull(principal.FindFirst(JwtRegisteredClaimNames.Jti));
        Assert.NotNull(principal.FindFirst(JwtRegisteredClaimNames.Iat));
        Assert.DoesNotContain(principal.Claims, claim => claim.Type is "role" or "roles");
    }

    [Fact]
    public void Generate_should_create_a_new_jti_for_each_token()
    {
        var generator = new JwtAccessTokenGenerator(
            Options.Create(ValidOptions()),
            new FixedTimeProvider(Now));

        var first = new JwtSecurityTokenHandler().ReadJwtToken(generator.Generate().AccessToken);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(generator.Generate().AccessToken);

        Assert.NotEqual(
            first.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value,
            second.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "Oficina.Api.Tests",
        Audience = "Oficina.Administration.Tests",
        SigningKey = "unit-tests-only-signing-key-at-least-32-bytes",
        ExpirationMinutes = 15
    };

    private static TokenValidationParameters ValidationParameters(JwtOptions options) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateLifetime = false,
        NameClaimType = JwtRegisteredClaimNames.Sub
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
