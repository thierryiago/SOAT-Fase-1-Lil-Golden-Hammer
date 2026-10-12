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
        Assert.Equal("Pedro - Orçamento aguardando aprovação", sender.Subject);
        Assert.True(sender.IsHtml);
        Assert.Contains($"Orçamento ID:</strong> {budget.Id}", sender.Body);
        Assert.Contains("<li>Filtro | Quantidade: 2 | Preço unitário: 10.00 | Total: 20.00</li>", sender.Body);
        Assert.Contains("<li>Troca de oleo | Preço unitário: 100.00</li>", sender.Body);
        Assert.Contains("Valor total:</strong> 120.00", sender.Body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/approveBudget?budgetId={budget.Id}\"",
            sender.Body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/rejectBudget?budgetId={budget.Id}\"",
            sender.Body);
        Assert.Contains(">Aprovar orçamento</a>", sender.Body);
        Assert.Contains(">Rejeitar orçamento</a>", sender.Body);
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

        Assert.Equal(2, sender.Body!.Split("<li>Nenhum</li>").Length - 1);
        Assert.Contains("Valor total:</strong> 0.00", sender.Body);
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
        Assert.Equal("Veículo pronto para retirada", sender.Subject);
        Assert.Contains("Olá, Pedro!", sender.Body);
        Assert.Contains("Seu veículo está pronto para retirada na oficina.", sender.Body);
        Assert.Contains("- Placa: ABC-1234", sender.Body);
        Assert.Contains("- Marca: Fiat", sender.Body);
        Assert.Contains("- Modelo: Uno", sender.Body);
        Assert.Contains("- Ano: 2020", sender.Body);
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
        Assert.Equal("Pedro - Orçamento aguardando aprovação", sender.Subject);
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
        Assert.Equal("Veículo pronto para retirada", sender.Subject);
        Assert.False(sender.IsHtml);
    }

    private static NotificationEmailSender CreateNotificationEmailSender(IEmailTransport emailTransport) =>
        new(
            new SendEmailNotification(emailTransport),
            new SendBudgetAwaitingApproval(emailTransport),
            new SendVehicleReadyForPickup(emailTransport),
            new SendServiceOrderStatus(emailTransport));

    [Fact]
    public async Task NotificationEmailSender_should_delegate_status_notification()
    {
        var sender = new FakeEmailSender();
        var notifications = CreateNotificationEmailSender(sender);
        var context = new ServiceOrderStatusEmail(Guid.NewGuid(), "Pedro", "pedro@example.com",
            "ABC-1234", "Fiat", "Uno", 2020, Oficina.Domain.ServiceOrders.ServiceOrderStatus.Created,
            Oficina.Domain.ServiceOrders.ServiceOrderStatus.Received);

        await notifications.SendServiceOrderStatusAsync(context, CancellationToken.None);

        Assert.Equal(context.CustomerEmail, sender.Recipient);
        Assert.Contains(context.ServiceOrderId.ToString(), sender.Subject);
        Assert.Contains("Status atual: Recebida", sender.Body);
        Assert.False(sender.IsHtml);
    }

    [Fact]
    public async Task Budget_email_should_encode_customer_vehicle_and_catalog_text_as_html()
    {
        var sender = new FakeEmailSender();
        var context = new ServiceOrderStatusEmail(Guid.NewGuid(), "<script>cliente</script>", "cliente@example.com",
            "ABC-1234", "<b>marca</b>", "Uno", 2020, Oficina.Domain.ServiceOrders.ServiceOrderStatus.InDiagnosis,
            Oficina.Domain.ServiceOrders.ServiceOrderStatus.AwaitingApproval);
        var budget = new BudgetResponse(Guid.NewGuid(), Guid.NewGuid(), context.ServiceOrderId,
            DateTimeOffset.UnixEpoch, null, 10m,
            [new BudgetPartResponse(Guid.NewGuid(), Guid.NewGuid(), "<script>peça</script>", 1, 10m)], []);

        await new SendBudgetAwaitingApproval(sender).SendBudgetAwaitingApprovalAsync(context.CustomerName,
            context.CustomerEmail, budget, CancellationToken.None, context);

        Assert.DoesNotContain("<script>", sender.Body);
        Assert.DoesNotContain("<b>marca</b>", sender.Body);
        Assert.Contains("&lt;script&gt;", sender.Body);
        Assert.Contains("&lt;b&gt;marca&lt;/b&gt;", sender.Body);
    }

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
