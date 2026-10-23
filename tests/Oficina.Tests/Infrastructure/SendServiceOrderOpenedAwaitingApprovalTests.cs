using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using Oficina.Infrastructure.Notifications;

namespace Oficina.Tests.Infrastructure;

public sealed class SendServiceOrderOpenedAwaitingApprovalTests
{
    [Fact]
    public async Task Should_send_html_email_with_every_opening_data_and_decision_buttons()
    {
        var transport = new FakeEmailTransport();
        var sender = new SendServiceOrderOpenedAwaitingApproval(transport);
        var email = CreateEmail();

        await sender.SendAsync(email, CancellationToken.None);

        Assert.Equal("pedro@example.com", transport.Recipient);
        Assert.Equal("Pedro - OS aberta: orçamento aguardando aprovação", transport.Subject);
        Assert.True(transport.IsHtml);
        var body = transport.Body!;
        Assert.Contains($"<strong>ID:</strong> {email.ServiceOrderId}", body);
        Assert.Contains("<strong>Descrição:</strong> Revisao completa", body);
        Assert.Contains($"<strong>Orçamento ID:</strong> {email.Budget.Id}", body);
        Assert.Contains("<strong>Nome:</strong> Pedro", body);
        Assert.Contains("<strong>Documento:</strong> 52998224725", body);
        Assert.Contains("<strong>E-mail:</strong> pedro@example.com", body);
        Assert.Contains("<strong>Telefone:</strong> 11999999999", body);
        Assert.Contains("<strong>Placa:</strong> ABC1D23", body);
        Assert.Contains("<strong>Marca:</strong> Fiat", body);
        Assert.Contains("<strong>Modelo:</strong> Uno", body);
        Assert.Contains("<strong>Ano:</strong> 2020", body);
        Assert.Contains("<li>Troca de oleo | Preço unitário: 100.00</li>", body);
        Assert.Contains("<li>Filtro | Quantidade: 2 | Preço unitário: 10.00 | Total: 20.00</li>", body);
        Assert.Contains("<strong>Valor total:</strong> 120.00", body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/approveBudget?budgetId={email.Budget.Id}\"",
            body);
        Assert.Contains(
            $"href=\"https://localhost:5001/api/v1/notifications/rejectBudget?budgetId={email.Budget.Id}\"",
            body);
    }

    [Fact]
    public async Task Should_show_no_parts_when_the_order_has_only_services()
    {
        var transport = new FakeEmailTransport();
        var sender = new SendServiceOrderOpenedAwaitingApproval(transport);
        var email = CreateEmail();
        email = email with { Budget = email.Budget with { Parts = [] } };

        await sender.SendAsync(email, CancellationToken.None);

        Assert.Contains("<h3>Peças</h3>\r\n<ul>\r\n<li>Nenhuma</li>".ReplaceLineEndings(), transport.Body!.ReplaceLineEndings());
    }

    [Fact]
    public async Task Should_html_encode_user_provided_values()
    {
        var transport = new FakeEmailTransport();
        var sender = new SendServiceOrderOpenedAwaitingApproval(transport);
        var email = CreateEmail() with
        {
            CustomerName = "<script>alert(1)</script>",
            Description = "<b>urgente</b>",
            VehicleBrand = "<i>marca</i>"
        };

        await sender.SendAsync(email, CancellationToken.None);

        var body = transport.Body!;
        Assert.DoesNotContain("<script>", body);
        Assert.DoesNotContain("<b>urgente</b>", body);
        Assert.DoesNotContain("<i>marca</i>", body);
        Assert.Contains("&lt;script&gt;", body);
        Assert.Contains("&lt;b&gt;urgente&lt;/b&gt;", body);
        Assert.Contains("&lt;i&gt;marca&lt;/i&gt;", body);
    }

    [Fact]
    public async Task NotificationEmailSender_should_delegate_service_order_opened_email()
    {
        var transport = new FakeEmailTransport();
        var notifications = new NotificationEmailSender(
            new SendEmailNotification(transport),
            new SendBudgetAwaitingApproval(transport),
            new SendVehicleReadyForPickup(transport),
            new SendServiceOrderStatus(transport),
            new SendServiceOrderOpenedAwaitingApproval(transport));
        var email = CreateEmail();

        await notifications.SendServiceOrderOpenedAsync(email, CancellationToken.None);

        Assert.Equal("pedro@example.com", transport.Recipient);
        Assert.Equal("Pedro - OS aberta: orçamento aguardando aprovação", transport.Subject);
        Assert.Contains($"<strong>ID:</strong> {email.ServiceOrderId}", transport.Body);
    }

    [Fact]
    public async Task NotificationEmailSender_should_keep_the_existing_budget_email_unchanged()
    {
        var transport = new FakeEmailTransport();
        var notifications = new NotificationEmailSender(
            new SendEmailNotification(transport),
            new SendBudgetAwaitingApproval(transport),
            new SendVehicleReadyForPickup(transport),
            new SendServiceOrderStatus(transport),
            new SendServiceOrderOpenedAwaitingApproval(transport));
        var budget = CreateEmail().Budget;

        await notifications.SendBudgetAwaitingApprovalAsync("Pedro", "pedro@example.com", budget, CancellationToken.None);

        Assert.Equal("Pedro - Orçamento aguardando aprovação", transport.Subject);
        Assert.DoesNotContain("OS aberta", transport.Body);
    }

    [Fact]
    public async Task NotificationEmailSender_should_reject_opened_email_when_sender_is_not_configured()
    {
        var transport = new FakeEmailTransport();
        var notifications = new NotificationEmailSender(
            new SendEmailNotification(transport),
            new SendBudgetAwaitingApproval(transport),
            new SendVehicleReadyForPickup(transport),
            new SendServiceOrderStatus(transport));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            notifications.SendServiceOrderOpenedAsync(CreateEmail(), CancellationToken.None));

        Assert.Equal("Service order opened email sender is not configured.", exception.Message);
        Assert.Null(transport.Recipient);
    }

    private static ServiceOrderOpenedEmail CreateEmail()
    {
        var serviceOrderId = Guid.NewGuid();
        var budget = new BudgetResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            serviceOrderId,
            new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero),
            null,
            120m,
            [new BudgetPartResponse(Guid.NewGuid(), Guid.NewGuid(), "Filtro", 2, 10m)],
            [new BudgetWorkshopServiceResponse(Guid.NewGuid(), Guid.NewGuid(), "Troca de oleo", 100m)]);

        return new ServiceOrderOpenedEmail(
            serviceOrderId,
            "Revisao completa",
            new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero),
            "Pedro",
            "52998224725",
            "pedro@example.com",
            "11999999999",
            "ABC1D23",
            "Fiat",
            "Uno",
            2020,
            budget);
    }

    private sealed class FakeEmailTransport : IEmailTransport
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
}
