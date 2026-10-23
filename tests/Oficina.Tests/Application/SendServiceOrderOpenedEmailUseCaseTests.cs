using Microsoft.Extensions.Logging;
using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using Oficina.Application.Notifications.UseCases;
using Oficina.Domain.Customers;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Vehicles;

namespace Oficina.Tests.Application;

public sealed class SendServiceOrderOpenedEmailUseCaseTests
{
    [Fact]
    public async Task Should_send_email_with_every_opening_data()
    {
        var (serviceOrder, customer, vehicle, budget) = CreateData();
        var sender = new FakeOpenedEmailSender();
        var useCase = new SendServiceOrderOpenedEmailUseCase(sender, new CapturingLogger());

        await useCase.ExecuteAsync(serviceOrder, customer, vehicle, budget, CancellationToken.None);

        var email = Assert.Single(sender.Sent);
        Assert.Equal(serviceOrder.Id, email.ServiceOrderId);
        Assert.Equal(serviceOrder.Description, email.Description);
        Assert.Equal(serviceOrder.CreatedAt, email.CreatedAt);
        Assert.Equal(customer.Name, email.CustomerName);
        Assert.Equal(customer.Document, email.CustomerDocument);
        Assert.Equal(customer.Email, email.CustomerEmail);
        Assert.Equal(customer.TelephoneNumber, email.CustomerTelephoneNumber);
        Assert.Equal(vehicle.Plate, email.VehiclePlate);
        Assert.Equal(vehicle.Brand, email.VehicleBrand);
        Assert.Equal(vehicle.Model, email.VehicleModel);
        Assert.Equal(vehicle.Year, email.VehicleYear);
        Assert.Same(budget, email.Budget);
    }

    [Fact]
    public async Task Should_log_without_personal_data_and_not_throw_when_sender_fails()
    {
        var (serviceOrder, customer, vehicle, budget) = CreateData();
        var sender = new FakeOpenedEmailSender { Failure = new InvalidOperationException($"SMTP refused {customer.Email}") };
        var logger = new CapturingLogger();
        var useCase = new SendServiceOrderOpenedEmailUseCase(sender, logger);

        await useCase.ExecuteAsync(serviceOrder, customer, vehicle, budget, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(serviceOrder.Id.ToString(), entry.Message);
        Assert.Contains(nameof(InvalidOperationException), entry.Message);
        Assert.DoesNotContain(customer.Email, entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task Should_propagate_cancellation()
    {
        var (serviceOrder, customer, vehicle, budget) = CreateData();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var sender = new FakeOpenedEmailSender { Failure = new OperationCanceledException(cancellation.Token) };
        var useCase = new SendServiceOrderOpenedEmailUseCase(sender, new CapturingLogger());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(serviceOrder, customer, vehicle, budget, cancellation.Token));
    }

    private static (ServiceOrder, Customer, Vehicle, BudgetResponse) CreateData()
    {
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Fiat", "Uno", 2020, EnumVehicleCategory.Car);
        var serviceOrder = ServiceOrder.Open(customer.Id, vehicle.Id, "Revisao completa");
        var budget = new BudgetResponse(
            Guid.NewGuid(),
            customer.Id,
            serviceOrder.Id,
            DateTimeOffset.UtcNow,
            null,
            100m,
            [],
            [new BudgetWorkshopServiceResponse(Guid.NewGuid(), Guid.NewGuid(), "Troca de oleo", 100m)]);
        return (serviceOrder, customer, vehicle, budget);
    }

    private sealed class FakeOpenedEmailSender : INotificationEmailSender
    {
        public List<ServiceOrderOpenedEmail> Sent { get; } = [];
        public Exception? Failure { get; set; }

        public Task SendServiceOrderOpenedAsync(ServiceOrderOpenedEmail email, CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;
            Sent.Add(email);
            return Task.CompletedTask;
        }

        public Task SendServiceOrderStatusAsync(ServiceOrderStatusEmail context, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendBudgetAwaitingApprovalAsync(string customerName, string customerEmail, BudgetResponse budget,
            CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
            throw new NotSupportedException();

        public Task SendVehicleReadyForPickupAsync(string customerName, string customerEmail, string vehiclePlate,
            string vehicleBrand, string vehicleModel, int vehicleYear,
            CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingLogger : ILogger<SendServiceOrderOpenedEmailUseCase>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
