using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Notifications;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_should_register_dbcontext_and_repositories_as_scoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure("Host=localhost;Database=test;Username=test;Password=test", configuration);

        Type[] expectedServices =
        [
            typeof(ICustomerRepository),
            typeof(IVehicleRepository),
            typeof(IPartRepository),
            typeof(IStockRepository),
            typeof(IWorkshopServiceRepository),
            typeof(IServiceOrderRepository),
            typeof(IMechanicRepository),
            typeof(INotificationEmailSender),
        ];

        foreach (var serviceType in expectedServices)
        {
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == serviceType && descriptor.Lifetime == ServiceLifetime.Scoped);
        }

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(AppDbContext));
    }
}
