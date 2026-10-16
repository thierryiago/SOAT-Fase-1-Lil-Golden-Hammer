using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Api.Authentication;
using Oficina.Api.ContractTests.Infrastructure;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.Customers;
using Oficina.Domain.ServiceOrders;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Api.ContractTests.Domains;

public sealed class ServiceOrderStatusTests
{
    [Fact]
    public async Task Status_endpoint_should_return_ordered_summaries_and_exclude_other_statuses()
    {
        await using var factory = new OficinaApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var oldest = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedIds = new List<Guid>();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("Status Customer", "status@example.com", "11999990000", TestDocuments.ValidCpf(1));
            context.Customers.Add(customer);
            foreach (var status in Enum.GetValues<ServiceOrderStatus>().Reverse().Cast<ServiceOrderStatus?>().Append(null))
            {
                var order = ServiceOrder.Open(customer.Id, Guid.NewGuid(), $"Order {status}");
                context.Entry(order).Property(order => order.Status).CurrentValue = status;
                context.Entry(order).Property(order => order.CreatedAt).CurrentValue = oldest;
                context.ServiceOrders.Add(order);
                if (status is ServiceOrderStatus.InExecution or ServiceOrderStatus.AwaitingApproval
                    or ServiceOrderStatus.InDiagnosis or ServiceOrderStatus.Received)
                    expectedIds.Add(order.Id);
            }
            var newer = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Newer execution");
            context.Entry(newer).Property(order => order.Status).CurrentValue = ServiceOrderStatus.InExecution;
            context.Entry(newer).Property(order => order.CreatedAt).CurrentValue = oldest.AddDays(1);
            context.Entry(newer).Property(order => order.TotalParts).CurrentValue = 123.45m;
            context.ServiceOrders.Add(newer);
            expectedIds.Insert(1, newer.Id);
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/service-orders/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<ServiceOrderListItemResponse[]>();
        Assert.NotNull(orders);
        Assert.Equal(expectedIds, orders.Select(order => order.Id));
        Assert.Equal(new ServiceOrderStatus?[] { ServiceOrderStatus.InExecution, ServiceOrderStatus.InExecution,
            ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.InDiagnosis, ServiceOrderStatus.Received },
            orders.Select(order => order.Status));
        Assert.Equal(oldest, orders[0].CreatedAt);
        Assert.Equal("Newer execution", orders[1].Description);
        Assert.Equal(123.45m, orders[1].TotalParts);
        Assert.NotEqual(Guid.Empty, orders[1].CustomerId);
        Assert.NotNull(orders[1].VehicleId);
        Assert.Null(orders[1].MechanicId);
    }

    [Fact]
    public async Task Status_endpoint_should_return_empty_array_when_no_orders_are_eligible()
    {
        await using var factory = new OficinaApiFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Created order");
            context.ServiceOrders.Add(order);
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/service-orders/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Status_endpoint_should_require_authentication()
    {
        await using var factory = new OficinaApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/service-orders/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/v1/auth/token", null);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
