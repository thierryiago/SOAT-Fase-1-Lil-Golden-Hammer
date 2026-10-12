using Microsoft.Extensions.Logging;
using Oficina.Application.Notifications;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Domain.ServiceOrders;
using System.Net;
using System.Net.Mail;

namespace Oficina.Tests.Application;

public sealed partial class ServiceOrderContractTests
{
    [Theory]
    [InlineData(ServiceOrderStatus.Created, "Criada")]
    [InlineData(ServiceOrderStatus.Received, "Recebida")]
    [InlineData(ServiceOrderStatus.InDiagnosis, "Em diagnóstico")]
    [InlineData(ServiceOrderStatus.AwaitingApproval, "Aguardando aprovação")]
    [InlineData(ServiceOrderStatus.InExecution, "Em execução")]
    [InlineData(ServiceOrderStatus.Finalized, "Finalizada")]
    [InlineData(ServiceOrderStatus.Delivered, "Entregue")]
    [InlineData(ServiceOrderStatus.Rejected, "Rejeitada")]
    public async Task Status_event_should_send_one_email_with_order_vehicle_and_current_status(
        ServiceOrderStatus status, string statusName)
    {
        var context = await PrepareBeforeStatusAsync(status);
        var before = context.EmailSender.SendCount;
        if (status != ServiceOrderStatus.Created)
            await ReachStatusAsync(context, status);

        Assert.Equal(status == ServiceOrderStatus.Created ? 1 : before + 1, context.EmailSender.SendCount);
        var email = context.EmailSender.Messages.Last();
        Assert.Equal("john@email.com", email.Recipient);
        var body = WebUtility.HtmlDecode(email.Body);
        Assert.Contains("Olá, John Customer!", body);
        Assert.Contains(context.ServiceOrderId.ToString(), body);
        Assert.Contains("ABC-1234", body);
        Assert.Contains("Fiat", body);
        Assert.Contains("Uno", body);
        Assert.Contains($"Status atual: {statusName}", body);
        Assert.Equal(status == ServiceOrderStatus.AwaitingApproval, email.IsHtml);
        if (status == ServiceOrderStatus.AwaitingApproval)
            Assert.Contains("approveBudget?budgetId=", email.Body);
        if (status == ServiceOrderStatus.Finalized)
            Assert.Equal("Veículo pronto para retirada", email.Subject);
    }

    [Theory]
    [InlineData(ServiceOrderStatus.Created)]
    [InlineData(ServiceOrderStatus.Received)]
    [InlineData(ServiceOrderStatus.InDiagnosis)]
    [InlineData(ServiceOrderStatus.AwaitingApproval)]
    [InlineData(ServiceOrderStatus.InExecution)]
    [InlineData(ServiceOrderStatus.Finalized)]
    [InlineData(ServiceOrderStatus.Delivered)]
    [InlineData(ServiceOrderStatus.Rejected)]
    public async Task Smtp_failure_should_log_and_preserve_each_status_event(ServiceOrderStatus status)
    {
        var failure = new SmtpException("SMTP failure containing john@email.com");
        var context = status == ServiceOrderStatus.Created
            ? await CreateOpenedOrderAsync(emailFailure: failure)
            : await PrepareBeforeStatusAsync(status);
        var before = context.EmailSender.SendCount;
        if (status != ServiceOrderStatus.Created)
        {
            context.EmailSender.Failure = failure;
            await ReachStatusAsync(context, status);
        }

        var order = await context.Orders.GetByIdAsync(context.ServiceOrderId, CancellationToken.None);
        Assert.Equal(status, order!.Status);
        Assert.Equal(before, context.EmailSender.SendCount);
        var log = Assert.Single(context.Logger.Entries);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Null(log.Exception);
        Assert.Equal(context.ServiceOrderId, log.Fields["OrderId"]);
        Assert.Equal(status, log.Fields["Status"]);
        ServiceOrderStatus? previousStatus = status switch
        {
            ServiceOrderStatus.Created => null,
            ServiceOrderStatus.Rejected => ServiceOrderStatus.AwaitingApproval,
            _ => (ServiceOrderStatus)((int)status - 1)
        };
        Assert.Equal(previousStatus, log.Fields["PreviousStatus"]);
        Assert.Equal(nameof(SmtpException), log.Fields["FailureType"]);
        Assert.DoesNotContain("john@email.com", log.Message);
        if (status != ServiceOrderStatus.Created)
        {
            var history = await context.History.FindByServiceOrderAsync(context.ServiceOrderId, CancellationToken.None);
            Assert.Equal(status.ToString(), history.Last().StatusName);
        }
        if (status is ServiceOrderStatus.AwaitingApproval or ServiceOrderStatus.InExecution or ServiceOrderStatus.Rejected)
        {
            var budget = await context.Budgets.GetByServiceOrderIdAsync(context.ServiceOrderId, CancellationToken.None);
            Assert.NotNull(budget);
            Assert.Equal(status == ServiceOrderStatus.AwaitingApproval ? (bool?)null : status == ServiceOrderStatus.InExecution, budget.IsApproved);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reapproval_should_create_new_budget_and_notify_once_even_when_smtp_fails(bool smtpFails)
    {
        var context = await PrepareBeforeStatusAsync(ServiceOrderStatus.Finalized);
        var before = context.EmailSender.SendCount;
        var oldBudget = Assert.Single(await context.Budgets.ListAsync(CancellationToken.None));
        if (smtpFails) context.EmailSender.Failure = new SmtpException();

        await context.UpdateServiceOrder.ExecuteAsync(new UpdateServiceOrderRequest(context.ServiceOrderId,
            Parts: [new AddPartToServiceOrderRequest(context.PartId, 2)]), CancellationToken.None);

        Assert.Equal(2, (await context.Budgets.ListAsync(CancellationToken.None)).Count);
        Assert.True(oldBudget.IsApproved);
        var latest = await context.Budgets.GetByServiceOrderIdAsync(context.ServiceOrderId, CancellationToken.None);
        Assert.NotEqual(oldBudget.Id, latest!.Id);
        Assert.Null(latest.IsApproved);
        Assert.Equal(8, (await context.Stocks.GetByPartIdAsync(context.PartId, CancellationToken.None))!.Quantity);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval,
            (await context.Orders.GetByIdAsync(context.ServiceOrderId, CancellationToken.None))!.Status);
        Assert.Equal(before + (smtpFails ? 0 : 1), context.EmailSender.SendCount);
        if (smtpFails)
            Assert.Single(context.Logger.Entries);
        else
        {
            var body = WebUtility.HtmlDecode(context.EmailSender.Body);
            Assert.Contains("novo orçamento exige sua aprovação", body);
            Assert.Contains(latest.Id.ToString(), body);
        }
    }

    [Fact]
    public async Task Rejection_smtp_failure_should_preserve_stock_return_and_budget_decision()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);
        await context.UpdateServiceOrder.ExecuteAsync(new UpdateServiceOrderRequest(context.ServiceOrderId,
            Parts: [new AddPartToServiceOrderRequest(context.PartId, 2)],
            WorkshopServiceIds: [context.WorkshopServiceId]), CancellationToken.None);
        context.EmailSender.Failure = new SmtpException();

        await context.CancelServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(10, (await context.Stocks.GetByPartIdAsync(context.PartId, CancellationToken.None))!.Quantity);
        Assert.False((await context.Budgets.GetByServiceOrderIdAsync(context.ServiceOrderId, CancellationToken.None))!.IsApproved);
        Assert.Single(context.Logger.Entries);
    }

    [Fact]
    public async Task Unchanged_status_invalid_action_and_persistence_failure_should_not_notify()
    {
        var context = await CreateOpenedOrderAsync();
        var before = context.EmailSender.SendCount;
        await context.UpdateServiceOrder.ExecuteAsync(new UpdateServiceOrderRequest(context.ServiceOrderId,
            Description: "Descrição atualizada"), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.FinalizeServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None));
        context.Orders.PersistenceFailure = new InvalidOperationException("Persistence failed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.UpdateServiceOrder.ExecuteAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, CheckList: "Checklist"), CancellationToken.None));
        Assert.Equal(before, context.EmailSender.SendCount);
        Assert.Empty(context.Logger.Entries);
        Assert.Empty(await context.History.FindByServiceOrderAsync(context.ServiceOrderId, CancellationToken.None));
    }

    [Fact]
    public async Task Opening_persistence_failure_should_not_notify()
    {
        var context = await CreateOpenedOrderAsync();
        var order = (await context.Orders.GetByIdAsync(context.ServiceOrderId, CancellationToken.None))!;
        var notifier = new ServiceOrderStatusNotifier(context.Customers, context.Vehicles,
            CreateNotificationEmailSender(context.EmailSender), context.Logger);
        var open = new OpenServiceOrderUseCase(context.Orders, context.Customers, context.Vehicles, notifier);
        context.Orders.PersistenceFailure = new InvalidOperationException("Persistence failed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => open.ExecuteAsync(
            new OpenServiceOrderRequest(order.CustomerId, order.VehicleId!.Value, "Nova OS"), CancellationToken.None));

        Assert.Single(context.EmailSender.Messages);
        Assert.Empty(context.Logger.Entries);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task History_or_budget_failure_should_propagate_without_sending_email(bool historyFails)
    {
        var context = await PrepareBeforeStatusAsync(ServiceOrderStatus.AwaitingApproval);
        var before = context.EmailSender.SendCount;
        var failure = new InvalidOperationException("Persistence failed");
        if (historyFails) context.History.PersistenceFailure = failure;
        else context.Budgets.PersistenceFailure = failure;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ReachStatusAsync(context, ServiceOrderStatus.AwaitingApproval));

        Assert.Same(failure, exception);
        Assert.Equal(before, context.EmailSender.SendCount);
        Assert.Empty(context.Logger.Entries);
    }

    [Fact]
    public async Task Another_approval_after_reapproval_should_send_a_new_execution_email()
    {
        var context = await PrepareBeforeStatusAsync(ServiceOrderStatus.Finalized);
        await context.UpdateServiceOrder.ExecuteAsync(new UpdateServiceOrderRequest(context.ServiceOrderId,
            Parts: [new AddPartToServiceOrderRequest(context.PartId, 2)]), CancellationToken.None);

        await context.ApproveServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(7, context.EmailSender.SendCount);
        Assert.Equal(2, context.EmailSender.Messages.Count(email => email.Body.Contains("Status atual: Em execução")));
    }

    [Fact]
    public async Task Notification_should_propagate_requested_cancellation_and_token_to_transport()
    {
        var context = await CreateOpenedOrderAsync();
        var order = (await context.Orders.GetByIdAsync(context.ServiceOrderId, CancellationToken.None))!;
        var notifier = new ServiceOrderStatusNotifier(context.Customers, context.Vehicles,
            CreateNotificationEmailSender(context.EmailSender), context.Logger);
        using var cancellation = new CancellationTokenSource();
        await notifier.NotifyAsync(order, null, cancellation.Token);
        Assert.Equal(cancellation.Token, context.EmailSender.LastToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notifier.NotifyAsync(order, null, cancellation.Token));
        Assert.Empty(context.Logger.Entries);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_notification_customer_or_vehicle_should_log_without_throwing(bool customerMissing)
    {
        var context = await CreateOpenedOrderAsync();
        var order = (await context.Orders.GetByIdAsync(context.ServiceOrderId, CancellationToken.None))!;
        var notifier = new ServiceOrderStatusNotifier(
            customerMissing ? new FakeCustomerRepository() : context.Customers,
            new FakeVehicleRepository(), CreateNotificationEmailSender(context.EmailSender), context.Logger);

        await notifier.NotifyAsync(order, null, CancellationToken.None);

        Assert.Single(context.EmailSender.Messages);
        Assert.Equal(nameof(InvalidOperationException), Assert.Single(context.Logger.Entries).Fields["FailureType"]);
    }

    private static async Task<ServiceOrderTestContext> PrepareBeforeStatusAsync(ServiceOrderStatus status)
    {
        var context = await CreateOpenedOrderAsync();
        if (status is ServiceOrderStatus.Created or ServiceOrderStatus.Received) return context;
        await ReachStatusAsync(context, ServiceOrderStatus.Received);
        if (status == ServiceOrderStatus.InDiagnosis) return context;
        await ReachStatusAsync(context, ServiceOrderStatus.InDiagnosis);
        if (status == ServiceOrderStatus.AwaitingApproval) return context;
        await ReachStatusAsync(context, ServiceOrderStatus.AwaitingApproval);
        if (status is ServiceOrderStatus.InExecution or ServiceOrderStatus.Rejected) return context;
        await ReachStatusAsync(context, ServiceOrderStatus.InExecution);
        if (status == ServiceOrderStatus.Finalized) return context;
        await ReachStatusAsync(context, ServiceOrderStatus.Finalized);
        return context;
    }

    private static Task<ServiceOrderDetailResponse> ReachStatusAsync(ServiceOrderTestContext context, ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Received => context.UpdateServiceOrder.ExecuteAsync(new(context.ServiceOrderId, CheckList: "Checklist"), CancellationToken.None),
        ServiceOrderStatus.InDiagnosis => context.UpdateServiceOrder.ExecuteAsync(new(context.ServiceOrderId, MechanicId: Guid.NewGuid()), CancellationToken.None),
        ServiceOrderStatus.AwaitingApproval => context.UpdateServiceOrder.ExecuteAsync(new(context.ServiceOrderId, WorkshopServiceIds: [context.WorkshopServiceId]), CancellationToken.None),
        ServiceOrderStatus.InExecution => context.ApproveServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None),
        ServiceOrderStatus.Finalized => context.FinalizeServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None),
        ServiceOrderStatus.Delivered => context.DeliverServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None),
        ServiceOrderStatus.Rejected => context.CancelServiceOrder.ExecuteAsync(context.ServiceOrderId, CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private sealed class CapturingNotificationLogger : ILogger<ServiceOrderStatusNotifier>
    {
        public List<(LogLevel Level, string Message, Exception? Exception, Dictionary<string, object?> Fields)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception,
                ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(pair => pair.Key, pair => pair.Value)));
    }
}
