using Microsoft.EntityFrameworkCore;
using Oficina.Domain.OrderService;

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
}
