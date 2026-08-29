using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Budget;
using Oficina.Domain.OrderService;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_can_be_built_with_service_order_part_entity()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(ServiceOrderPart));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void Budget_service_order_index_should_allow_multiple_versions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Budget));
        var index = entityType!.GetIndexes().Single(candidate =>
            candidate.Properties.Single().Name == nameof(Budget.ServiceOrderId));

        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Migrations_should_include_multiple_budget_versions_change()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        Assert.Contains(
            "20260829120000_AllowMultipleBudgetsPerServiceOrder",
            context.Database.GetMigrations());
    }
}
