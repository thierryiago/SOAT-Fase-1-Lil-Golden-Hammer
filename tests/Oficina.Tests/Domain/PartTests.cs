using Oficina.Domain.Parts;

namespace Oficina.Tests.Domain;

public sealed class PartTests
{
    [Fact]
    public void Create_should_normalize_code_and_trim_name()
    {
        var part = Part.Create("  Filtro de Oleo  ", " flt-001 ", 35.5m, EnumPartKind.Part);

        Assert.Equal("Filtro de Oleo", part.Name);
        Assert.Equal("FLT-001", part.Code);
        Assert.Equal(35.5m, part.UnitPrice);
        Assert.Equal(EnumPartKind.Part, part.Kind);
        Assert.True(part.IsActive);
    }

    [Fact]
    public void Create_should_default_kind_to_part()
    {
        var part = Part.Create("Oleo 5W30", "OIL-001", 60m);

        Assert.Equal(EnumPartKind.Part, part.Kind);
    }

    [Fact]
    public void Create_should_reject_empty_name()
    {
        var act = () => Part.Create(" ", "COD-001", 10m, EnumPartKind.Part);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_empty_code()
    {
        var act = () => Part.Create("Filtro", " ", 10m, EnumPartKind.Part);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_negative_unit_price()
    {
        var act = () => Part.Create("Filtro", "COD-001", -1m, EnumPartKind.Part);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Update_should_change_name_code_price_and_kind()
    {
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);

        part.Update("Filtro de Ar", " cod-002 ", 15m, EnumPartKind.Consumable);

        Assert.Equal("Filtro de Ar", part.Name);
        Assert.Equal("COD-002", part.Code);
        Assert.Equal(15m, part.UnitPrice);
        Assert.Equal(EnumPartKind.Consumable, part.Kind);
        Assert.True(part.UpdateDate > default(DateTime));
    }

    [Fact]
    public void Update_should_reject_negative_unit_price()
    {
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);

        var act = () => part.Update("Filtro", "COD-001", -5m, EnumPartKind.Part);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Deactivate_should_set_is_active_to_false()
    {
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);

        part.Deactivate();

        Assert.False(part.IsActive);
    }
}
