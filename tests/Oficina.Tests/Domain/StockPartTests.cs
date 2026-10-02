using Oficina.Domain.Stock;

namespace Oficina.Tests.Domain;

public sealed class StockPartTests
{
    [Fact]
    public void Create_should_reject_empty_part_id()
    {
        var act = () => StockPart.Create(Guid.Empty, 10);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_negative_quantity()
    {
        var act = () => StockPart.Create(Guid.NewGuid(), -1);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_allow_zero_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 0);

        Assert.Equal(0, stock.Quantity);
    }

    [Fact]
    public void AddQuantity_should_increase_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.AddQuantity(3);

        Assert.Equal(8, stock.Quantity);
    }

    [Fact]
    public void AddQuantity_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.AddQuantity(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void RemoveQuantity_should_decrease_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.RemoveQuantity(3);

        Assert.Equal(2, stock.Quantity);
    }

    [Fact]
    public void RemoveQuantity_should_reject_result_below_zero()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.RemoveQuantity(6);

        Assert.Throws<InsufficientStockException>(act);
    }

    [Fact]
    public void AdjustQuantity_should_apply_positive_delta()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.AdjustQuantity(4);

        Assert.Equal(9, stock.Quantity);
    }

    [Fact]
    public void AdjustQuantity_should_apply_negative_delta()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.AdjustQuantity(-2);

        Assert.Equal(3, stock.Quantity);
    }

    [Fact]
    public void AdjustQuantity_should_reject_result_below_zero()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.AdjustQuantity(-6);

        Assert.Throws<InsufficientStockException>(act);
    }

    [Fact]
    public void AdjustQuantity_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.AdjustQuantity(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Reserve_should_decrease_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.Reserve(3);

        Assert.Equal(2, stock.Quantity);
    }

    [Fact]
    public void Reserve_should_allow_consuming_the_whole_balance()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.Reserve(5);

        Assert.Equal(0, stock.Quantity);
    }

    [Fact]
    public void Reserve_should_reject_quantity_above_balance()
    {
        var partId = Guid.NewGuid();
        var stock = StockPart.Create(partId, 5);

        var exception = Assert.Throws<InsufficientStockException>(() => stock.Reserve(6));

        Assert.Equal(partId, exception.PartId);
        Assert.Equal(5, exception.Available);
        Assert.Equal(6, exception.Requested);
    }

    [Fact]
    public void Reserve_should_not_change_quantity_when_balance_is_insufficient()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        Assert.Throws<InsufficientStockException>(() => stock.Reserve(6));

        Assert.Equal(5, stock.Quantity);
    }

    [Fact]
    public void Reserve_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.Reserve(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Release_should_increase_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.Release(3);

        Assert.Equal(8, stock.Quantity);
    }

    [Fact]
    public void Release_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.Release(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void EnsureCanReserve_should_reject_quantity_above_balance_without_changing_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        Assert.Throws<InsufficientStockException>(() => stock.EnsureCanReserve(6));

        Assert.Equal(5, stock.Quantity);
    }

    [Fact]
    public void EnsureCanReserve_should_not_change_quantity_when_balance_is_enough()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.EnsureCanReserve(5);

        Assert.Equal(5, stock.Quantity);
    }

    [Fact]
    public void EnsureCanReserve_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.EnsureCanReserve(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SetQuantity_should_replace_quantity()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        stock.SetQuantity(20);

        Assert.Equal(20, stock.Quantity);
    }

    [Fact]
    public void SetQuantity_should_reject_negative_value()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.SetQuantity(-1);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}
