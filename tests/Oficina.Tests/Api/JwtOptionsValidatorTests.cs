using Oficina.Api.Authentication;

namespace Oficina.Tests.Api;

public sealed class JwtOptionsValidatorTests
{
    [Theory]
    [InlineData("", "A JWT signing key is required.")]
    [InlineData("short", "The JWT signing key must contain at least 32 UTF-8 bytes.")]
    public void Validate_should_reject_invalid_signing_key(string signingKey, string expectedError)
    {
        var options = ValidOptions();
        options.SigningKey = signingKey;

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(expectedError, result.Failures);
    }

    [Theory]
    [InlineData("", "Oficina.Administration", 15, "A JWT issuer is required.")]
    [InlineData("Oficina.Api", "", 15, "A JWT audience is required.")]
    [InlineData("Oficina.Api", "Oficina.Administration", 0, "JWT expiration must be at least one minute.")]
    public void Validate_should_reject_invalid_public_settings(
        string issuer,
        string audience,
        int expirationMinutes,
        string expectedError)
    {
        var options = ValidOptions();
        options.Issuer = issuer;
        options.Audience = audience;
        options.ExpirationMinutes = expirationMinutes;

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(expectedError, result.Failures);
    }

    [Fact]
    public void Validate_should_accept_valid_options()
    {
        var result = new JwtOptionsValidator().Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "Oficina.Api",
        Audience = "Oficina.Administration",
        SigningKey = "unit-tests-only-signing-key-at-least-32-bytes",
        ExpirationMinutes = 15
    };
}
