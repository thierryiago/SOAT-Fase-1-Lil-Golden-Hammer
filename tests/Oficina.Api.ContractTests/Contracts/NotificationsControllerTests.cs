using System.Net;
using System.Net.Http.Json;
using Oficina.Api.ContractTests.Infrastructure;

namespace Oficina.Api.ContractTests.Contracts;

public sealed class NotificationsControllerTests(OficinaApiFactory factory) : IClassFixture<OficinaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Send_email_should_return_no_content()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/email", new { email = "cliente@example.com" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Send_email_should_reject_invalid_address()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/email", new { email = "invalid-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
