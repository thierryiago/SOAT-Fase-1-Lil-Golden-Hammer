using Microsoft.Extensions.Options;
using Oficina.Api.ContractTests.Infrastructure;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class AppConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("localhost:8080")]
    [InlineData("ftp://oficina.example.test")]
    [InlineData("https://oficina.example.test/oficina")]
    [InlineData("https://oficina.example.test?source=email")]
    [InlineData("https://oficina.example.test#section")]
    public void Api_should_fail_at_startup_when_public_base_url_is_invalid(string? publicBaseUrl)
    {
        using var factory = new OficinaApiFactory(publicBaseUrl);

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
