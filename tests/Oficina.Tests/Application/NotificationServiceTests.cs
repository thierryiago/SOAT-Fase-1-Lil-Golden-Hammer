using Oficina.Application.Notifications;
using Oficina.Application.Budgets;

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

    [Fact]
    public async Task SendBudgetAwaitingApprovalAsync_should_send_budget_as_plain_text()
    {
        var sender = new FakeEmailSender();
        var service = new NotificationService(sender);
        var budget = new BudgetResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 27, 12, 30, 0, TimeSpan.Zero),
            null,
            120m,
            [new BudgetPartResponse(Guid.NewGuid(), Guid.NewGuid(), "Filtro", 2, 10m)],
            [new BudgetWorkshopServiceResponse(Guid.NewGuid(), Guid.NewGuid(), "Troca de oleo", 100m)]);

        await service.SendBudgetAwaitingApprovalAsync(
            "Pedro",
            "pedro@example.com",
            budget,
            CancellationToken.None);

        Assert.Equal("pedro@example.com", sender.Recipient);
        Assert.Equal("Pedro - Budget Awaiting to Approval", sender.Subject);
        Assert.Contains($"Budget ID: {budget.Id}", sender.Body);
        Assert.Contains("- Filtro | Quantity: 2 | Unit Price: 10.00 | Total: 20.00", sender.Body);
        Assert.Contains("- Troca de oleo | Unit Price: 100.00", sender.Body);
        Assert.Contains("Total Value: 120.00", sender.Body);
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
