using Oficina.Infrastructure.Notifications;

namespace Oficina.Tests.Infrastructure;

public sealed class AppOptionsValidatorTests
{
    [Theory]
    [InlineData("https://oficina.example.test")]
    [InlineData("https://oficina.example.test/")]
    [InlineData("http://localhost:8080")]
    public void Should_accept_http_origins(string publicBaseUrl)
    {
        var result = new AppOptionsValidator().Validate(null, new AppOptions { PublicBaseUrl = publicBaseUrl });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("https://user:pass@oficina.example.test")]
    [InlineData(" https://oficina.example.test")]
    [InlineData("https://oficina.example.test/oficina")]
    [InlineData("https://oficina.example.test/./")]
    [InlineData("https://oficina.example.test?source=email")]
    public void Should_reject_values_that_are_not_http_origins(string publicBaseUrl)
    {
        var result = new AppOptionsValidator().Validate(null, new AppOptions { PublicBaseUrl = publicBaseUrl });

        Assert.True(result.Failed);
    }
}
