using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using Oficina.Infrastructure.Notifications;

namespace Oficina.Tests.Infrastructure;

public sealed class NotificationsTests
{
    [Fact]
    public async Task SendEmailNotification_should_send_simple_notification_to_recipient()
    {
        var sender = new FakeEmailSender();
        var useCase = new SendEmailNotification(sender);

        await useCase.SendEmailAsync(new SendEmailNotificationRequest(" cliente@example.com "), CancellationToken.None);

        Assert.Equal("cliente@example.com", sender.Recipient);
        Assert.Equal("Notificação da Oficina", sender.Subject);
        Assert.Equal("Esta é uma notificação enviada pela Oficina.", sender.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task SendEmailNotification_should_reject_invalid_email(string email)
    {
        var useCase = new SendEmailNotification(new FakeEmailSender());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.SendEmailAsync(new SendEmailNotificationRequest(email), CancellationToken.None));
    }

    [Fact]
    public async Task SendEmailNotification_should_propagate_sender_failure()
    {
        var useCase = new SendEmailNotification(new FailingEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.SendEmailAsync(new SendEmailNotificationRequest("cliente@example.com"), CancellationToken.None));
    }

    [Fact]
    public async Task SendBudgetAwaitingApproval_should_send_budget_as_html_with_decision_buttons()
    {
        var sender = new FakeEmailSender();
        var useCase = new SendBudgetAwaitingApproval(sender);
        var budget = new BudgetResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 27, 12, 30, 0, TimeSpan.Zero),
            null,
            120m,
            [new BudgetPartResponse(Guid.NewGuid(), Guid.NewGuid(), "Filtro", 2, 10m)],
            [new BudgetWorkshopServiceResponse(Guid.NewGuid(), Guid.NewGuid(), "Troca de oleo", 100m)]);

        await useCase.SendBudgetAwaitingApprovalAsync(
            "Pedro",
            "pedro@example.com",
            budget,
            CancellationToken.None);

        Assert.Equal("pedro@example.com", sender.Recipient);
        Assert.Equal("Pedro - Budget Awaiting to Approval", sender.Subject);
        Assert.True(sender.IsHtml);
        Assert.Contains($"Budget ID:</strong> {budget.Id}", sender.Body);
        Assert.Contains("<li>Filtro | Quantity: 2 | Unit Price: 10.00 | Total: 20.00</li>", sender.Body);
        Assert.Contains("<li>Troca de oleo | Unit Price: 100.00</li>", sender.Body);
        Assert.Contains("Total Value:</strong> 120.00", sender.Body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/approveBudget?budgetId={budget.Id}\"",
            sender.Body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/rejectBudget?budgetId={budget.Id}\"",
            sender.Body);
        Assert.Contains(">Approve Budget</a>", sender.Body);
        Assert.Contains(">Reject Budget</a>", sender.Body);
    }

    [Fact]
    public async Task SendBudgetAwaitingApproval_should_list_none_when_budget_has_no_items()
    {
        var sender = new FakeEmailSender();
        var useCase = new SendBudgetAwaitingApproval(sender);
        var budget = new BudgetResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 27, 12, 30, 0, TimeSpan.Zero),
            null,
            0m,
            [],
            []);

        await useCase.SendBudgetAwaitingApprovalAsync(
            "Pedro",
            "pedro@example.com",
            budget,
            CancellationToken.None);

        Assert.Equal(2, sender.Body!.Split("<li>None</li>").Length - 1);
        Assert.Contains("Total Value:</strong> 0.00", sender.Body);
    }

    [Fact]
    public async Task SendVehicleReadyForPickup_should_notify_customer()
    {
        var sender = new FakeEmailSender();
        var useCase = new SendVehicleReadyForPickup(sender);

        await useCase.SendVehicleReadyForPickupAsync(
            "Pedro",
            "pedro@example.com",
            "ABC-1234",
            "Fiat",
            "Uno",
            2020,
            CancellationToken.None);

        Assert.Equal("pedro@example.com", sender.Recipient);
        Assert.Equal("Vehicle ready for pickup", sender.Subject);
        Assert.Contains("Hello, Pedro!", sender.Body);
        Assert.Contains("Your vehicle is ready to be picked up at the workshop.", sender.Body);
        Assert.Contains("- Plate: ABC-1234", sender.Body);
        Assert.Contains("- Brand: Fiat", sender.Body);
        Assert.Contains("- Model: Uno", sender.Body);
        Assert.Contains("- Year: 2020", sender.Body);
    }

    [Fact]
    public async Task NotificationEmailSender_should_delegate_simple_notification()
    {
        var sender = new FakeEmailSender();
        var notifications = CreateNotificationEmailSender(sender);

        await notifications.SendEmailAsync(new SendEmailNotificationRequest("cliente@example.com"), CancellationToken.None);

        Assert.Equal("cliente@example.com", sender.Recipient);
        Assert.Equal("Notificação da Oficina", sender.Subject);
        Assert.False(sender.IsHtml);
    }

    [Fact]
    public async Task NotificationEmailSender_should_delegate_budget_awaiting_approval()
    {
        var sender = new FakeEmailSender();
        var notifications = CreateNotificationEmailSender(sender);
        var budget = new BudgetResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null, 0m, [], []);

        await notifications.SendBudgetAwaitingApprovalAsync("Pedro", "pedro@example.com", budget, CancellationToken.None);

        Assert.Equal("pedro@example.com", sender.Recipient);
        Assert.Equal("Pedro - Budget Awaiting to Approval", sender.Subject);
        Assert.True(sender.IsHtml);
    }

    [Fact]
    public async Task NotificationEmailSender_should_delegate_vehicle_ready_for_pickup()
    {
        var sender = new FakeEmailSender();
        var notifications = CreateNotificationEmailSender(sender);

        await notifications.SendVehicleReadyForPickupAsync(
            "Pedro",
            "pedro@example.com",
            "ABC-1234",
            "Fiat",
            "Uno",
            2020,
            CancellationToken.None);

        Assert.Equal("pedro@example.com", sender.Recipient);
        Assert.Equal("Vehicle ready for pickup", sender.Subject);
        Assert.False(sender.IsHtml);
    }

    private static NotificationEmailSender CreateNotificationEmailSender(IEmailTransport emailTransport) =>
        new(
            new SendEmailNotification(emailTransport),
            new SendBudgetAwaitingApproval(emailTransport),
            new SendVehicleReadyForPickup(emailTransport));

    private sealed class FakeEmailSender : IEmailTransport
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public bool IsHtml { get; private set; }

        public Task SendAsync(string recipient, string subject, string body, bool isHtml, CancellationToken cancellationToken)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            IsHtml = isHtml;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailSender : IEmailTransport
    {
        public Task SendAsync(string recipient, string subject, string body, bool isHtml, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("SMTP unavailable."));
    }
}
