using Oficina.Domain.Mechanics;

namespace Oficina.Tests.Domain;

public sealed class MechanicTests
{
    [Fact]
    public void Create_should_trim_name()
    {
        var mechanic = Mechanic.Create("  Joao  ");

        Assert.Equal("Joao", mechanic.Name);
        Assert.True(mechanic.IsActive);
    }

    [Fact]
    public void Create_should_reject_empty_name()
    {
        var act = () => Mechanic.Create(" ");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Update_should_trim_and_change_name()
    {
        var mechanic = Mechanic.Create("Joao");

        mechanic.Update("  Pedro  ");

        Assert.Equal("Pedro", mechanic.Name);
    }

    [Fact]
    public void Update_should_reject_empty_name()
    {
        var mechanic = Mechanic.Create("Joao");

        var act = () => mechanic.Update(" ");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Deactivate_should_set_is_active_to_false()
    {
        var mechanic = Mechanic.Create("Joao");

        mechanic.Deactivate();

        Assert.False(mechanic.IsActive);
    }
}
