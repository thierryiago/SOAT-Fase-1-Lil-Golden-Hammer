using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Domain;

public sealed class WorkshopServiceTests
{
    [Fact]
    public void Create_should_trim_name_and_description()
    {
        var service = WorkshopService.Create("  Troca de oleo  ", "  Troca de oleo do motor  ", 120m, 30);

        Assert.Equal("Troca de oleo", service.Name);
        Assert.Equal("Troca de oleo do motor", service.Description);
        Assert.Equal(120m, service.UnitPrice);
        Assert.Equal(30, service.EstimatedDurationMinutes);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Create_should_reject_empty_name()
    {
        var act = () => WorkshopService.Create(" ", "Descricao", 120m, 30);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_empty_description()
    {
        var act = () => WorkshopService.Create("Nome", " ", 120m, 30);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_reject_negative_unit_price()
    {
        var act = () => WorkshopService.Create("Nome", "Descricao", -1m, 30);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_reject_non_positive_duration()
    {
        var act = () => WorkshopService.Create("Nome", "Descricao", 120m, 0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Update_should_change_all_fields()
    {
        var service = WorkshopService.Create("Nome", "Descricao", 120m, 30);

        service.Update("Novo nome", "Nova descricao", 200m, 45);

        Assert.Equal("Novo nome", service.Name);
        Assert.Equal("Nova descricao", service.Description);
        Assert.Equal(200m, service.UnitPrice);
        Assert.Equal(45, service.EstimatedDurationMinutes);
    }

    [Fact]
    public void Update_should_reject_invalid_data()
    {
        var service = WorkshopService.Create("Nome", "Descricao", 120m, 30);

        var act = () => service.Update("Nome", "Descricao", 120m, -1);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Deactivate_should_set_is_active_to_false()
    {
        var service = WorkshopService.Create("Nome", "Descricao", 120m, 30);

        service.Deactivate();

        Assert.False(service.IsActive);
    }
}
