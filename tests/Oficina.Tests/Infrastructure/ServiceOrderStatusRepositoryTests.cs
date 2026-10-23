using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oficina.Domain.Customers;
using Oficina.Domain.ServiceOrderHistories;
using Oficina.Domain.ServiceOrders;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class ServiceOrderStatusRepositoryTests
{
    private static readonly DateTimeOffset Oldest = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListByStatusAsync_should_order_by_priority_then_creation_then_id_in_sql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commands = new QueryRecorder();
        await using var context = CreateContext(connection, commands);
        await context.Database.EnsureCreatedAsync();
        var customer = Customer.Create("Priority Customer", "priority@example.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        ServiceOrderStatus[] priorities = [ServiceOrderStatus.InExecution, ServiceOrderStatus.AwaitingApproval,
            ServiceOrderStatus.InDiagnosis, ServiceOrderStatus.Received];
        var expected = new List<Guid>();
        foreach (var (status, index) in priorities.Select((status, index) => (status, index)))
        {
            var firstId = Guid.Parse($"00000000-0000-0000-0000-{index * 3 + 1:000000000000}");
            var secondId = Guid.Parse($"00000000-0000-0000-0000-{index * 3 + 2:000000000000}");
            var thirdId = Guid.Parse($"00000000-0000-0000-0000-{index * 3 + 3:000000000000}");
            // Lower-priority groups are older, so a global date sort would fail.
            var createdAt = Oldest.AddDays(-index);
            AddOrder(context, customer.Id, status, createdAt.AddHours(1), thirdId);
            AddOrder(context, customer.Id, status, createdAt, secondId);
            AddOrder(context, customer.Id, status, createdAt, firstId);
            expected.AddRange([firstId, secondId, thirdId]);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        commands.Queries.Clear();

        var result = await new ServiceOrderRepository(context).ListByStatusAsync(CancellationToken.None);

        Assert.Equal(expected, result.Select(order => order.Id));
        Assert.Empty(context.ChangeTracker.Entries());
        var sql = Assert.Single(commands.Queries);
        Assert.Contains("WHERE", sql);
        Assert.Contains("ORDER BY CASE", sql);
        Assert.Contains("\"CreatedAt\"", sql);
        Assert.DoesNotContain("JOIN", sql);
    }

    [Theory]
    [InlineData(ServiceOrderStatus.Created)]
    [InlineData(ServiceOrderStatus.Rejected)]
    [InlineData(ServiceOrderStatus.Finalized)]
    [InlineData(ServiceOrderStatus.Delivered)]
    [InlineData(null)]
    public async Task ListByStatusAsync_should_exclude_ineligible_status_without_changing_or_deleting_order(
        ServiceOrderStatus? status)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, new QueryRecorder());
        await context.Database.EnsureCreatedAsync();
        var customer = Customer.Create("Excluded Customer", "excluded@example.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        var excluded = AddOrder(context, customer.Id, status, Oldest, Guid.NewGuid());
        var included = AddOrder(context, customer.Id, ServiceOrderStatus.Received, Oldest, Guid.NewGuid());
        var history = new ServiceOrderHistory(Guid.NewGuid(), excluded.Id, status?.ToString() ?? "Unknown", Oldest.UtcDateTime);
        context.ServiceOrderHistories.Add(history);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ServiceOrderRepository(context);

        var result = await repository.ListByStatusAsync(CancellationToken.None);

        Assert.Equal(included.Id, Assert.Single(result).Id);
        Assert.Equal(2, await context.ServiceOrders.CountAsync());
        var persisted = await repository.GetByIdAsync(excluded.Id, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(status, persisted.Status);
        var persistedHistory = await context.ServiceOrderHistories.AsNoTracking().SingleAsync();
        Assert.Equal(history.Id, persistedHistory.Id);
        Assert.Equal(history.StatusName, persistedHistory.StatusName);
        Assert.Equal(history.CreatedDate, persistedHistory.CreatedDate);
        Assert.Equal(2, (await repository.ListAsync(CancellationToken.None)).Count);
    }

    [Fact]
    public async Task ListByStatusAsync_should_return_empty_when_database_is_empty()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, new QueryRecorder());
        await context.Database.EnsureCreatedAsync();

        var result = await new ServiceOrderRepository(context).ListByStatusAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    private static ServiceOrder AddOrder(AppDbContext context, Guid customerId, ServiceOrderStatus? status,
        DateTimeOffset createdAt, Guid id)
    {
        var order = ServiceOrder.Open(customerId, Guid.NewGuid(), "Priority test");
        var entry = context.Entry(order);
        entry.Property(order => order.Id).CurrentValue = id;
        entry.Property(order => order.VehicleId).CurrentValue = null;
        entry.Property(order => order.CreatedAt).CurrentValue = createdAt;
        entry.Property(order => order.Status).CurrentValue = status;
        context.ServiceOrders.Add(order);
        return order;
    }

    private static SqliteTestContext CreateContext(SqliteConnection connection, QueryRecorder commands) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).AddInterceptors(commands).Options);

    private sealed class SqliteTestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // SQLite cannot order DateTimeOffset directly; production keeps its PostgreSQL mapping.
            modelBuilder.Entity<ServiceOrder>().Property(order => order.CreatedAt)
                .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        }
    }

    private sealed class QueryRecorder : DbCommandInterceptor
    {
        public List<string> Queries { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Queries.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
