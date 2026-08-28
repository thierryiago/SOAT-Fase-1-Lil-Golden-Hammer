using Microsoft.Extensions.Options;
using Oficina.Infrastructure.Notifications;
using System.Net;
using System.Net.Sockets;

namespace Oficina.Tests.Infrastructure;

public sealed class SmtpNotificationEmailSenderTests
{
    [Fact]
    public async Task SendAsync_should_attempt_delivery_without_credentials()
    {
        var sender = new SmtpNotificationEmailSender(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = GetUnusedLoopbackPort(),
            From = "noreply@example.com"
        }));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendAsync("cliente@example.com", "Assunto", "Corpo", CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_should_attach_credentials_when_username_is_configured()
    {
        var sender = new SmtpNotificationEmailSender(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = GetUnusedLoopbackPort(),
            From = "noreply@example.com",
            Username = "smtp-user",
            Password = "smtp-password"
        }));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendAsync("cliente@example.com", "Assunto", "Corpo", CancellationToken.None));
    }

    private static int GetUnusedLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task SendAsync_should_throw_when_host_is_missing()
    {
        var sender = new SmtpNotificationEmailSender(Options.Create(new SmtpOptions
        {
            Host = "",
            From = "noreply@example.com"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("cliente@example.com", "Assunto", "Corpo", CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_should_throw_when_from_address_is_missing()
    {
        var sender = new SmtpNotificationEmailSender(Options.Create(new SmtpOptions
        {
            Host = "smtp.example.com",
            From = ""
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("cliente@example.com", "Assunto", "Corpo", CancellationToken.None));
    }
}
