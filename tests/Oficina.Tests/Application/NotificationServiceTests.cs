using Oficina.Application.Notifications;

namespace Oficina.Tests.Application;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task SendEmailAsync_should_send_simple_notification_to_recipient()
    {
        var sender = new FakeEmailSender();
        var service = new NotificationService(sender);

        await service.SendEmailAsync(new SendEmailNotificationRequest(" cliente@example.com "), CancellationToken.None);

        Assert.Equal("cliente@example.com", sender.Recipient);
        Assert.Equal("Notificação da Oficina", sender.Subject);
        Assert.Equal("Esta é uma notificação enviada pela Oficina.", sender.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task SendEmailAsync_should_reject_invalid_email(string email)
    {
        var service = new NotificationService(new FakeEmailSender());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendEmailAsync(new SendEmailNotificationRequest(email), CancellationToken.None));
    }

    [Fact]
    public async Task SendEmailAsync_should_propagate_sender_failure()
    {
        var service = new NotificationService(new FailingEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendEmailAsync(new SendEmailNotificationRequest("cliente@example.com"), CancellationToken.None));
    }

    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }

        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailSender : INotificationEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("SMTP unavailable."));
    }
}
