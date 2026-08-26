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

        Assert.Throws<InvalidOperationException>(act);
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

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AdjustQuantity_should_reject_zero_movement()
    {
        var stock = StockPart.Create(Guid.NewGuid(), 5);

        var act = () => stock.AdjustQuantity(0);

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
