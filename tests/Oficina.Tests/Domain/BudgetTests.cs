using Oficina.Domain.Budget;
using Oficina.Domain.Parts;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Domain;

public sealed class BudgetTests
{
    [Fact]
    public void Open_should_reject_empty_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { BudgetWorkshopServices.Create(Guid.NewGuid(), Guid.NewGuid()) };

        var act = () => Budget.Open(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, workshopServices);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_empty_customer_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { BudgetWorkshopServices.Create(Guid.NewGuid(), Guid.NewGuid()) };

        var act = () => Budget.Open(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, workshopServices);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_empty_service_order_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { BudgetWorkshopServices.Create(Guid.NewGuid(), Guid.NewGuid()) };

        var act = () => Budget.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, workshopServices);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_null_workshop_services()
    {
        var act = () => Budget.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null!);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_empty_workshop_services()
    {
        var act = () => Budget.Open(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Array.Empty<BudgetWorkshopServices>());

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_calculate_total_value_from_parts_and_workshop_services()
    {
        var budgetId = Guid.NewGuid();
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        var budgetPart = BudgetParts.Create(budgetId, part.Id, 3);
        budgetPart.Part = part;

        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopService.Id);
        budgetWorkshopService.WorkshopService = workshopService;

        var budget = Budget.Open(
            budgetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<BudgetParts> { budgetPart },
            new List<BudgetWorkshopServices> { budgetWorkshopService });

        Assert.Equal(130m, budget.TotalValue);
        Assert.Null(budget.IsApproved);
    }

    [Fact]
    public void Open_should_default_total_value_to_workshop_services_when_parts_are_null()
    {
        var budgetId = Guid.NewGuid();
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopService.Id);
        budgetWorkshopService.WorkshopService = workshopService;

        var budget = Budget.Open(
            budgetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new List<BudgetWorkshopServices> { budgetWorkshopService });

        Assert.Equal(100m, budget.TotalValue);
        Assert.Empty(budget.Parts);
    }
}

public sealed class BudgetPartsTests
{
    [Fact]
    public void Create_should_reject_non_positive_quantity()
    {
        var act = () => BudgetParts.Create(Guid.NewGuid(), Guid.NewGuid(), 0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_set_ids_and_quantity()
    {
        var budgetId = Guid.NewGuid();
        var partId = Guid.NewGuid();

        var budgetPart = BudgetParts.Create(budgetId, partId, 5);

        Assert.Equal(budgetId, budgetPart.BudgetId);
        Assert.Equal(partId, budgetPart.PartId);
        Assert.Equal(5, budgetPart.Quantity);
    }
}

public sealed class BudgetWorkshopServicesTests
{
    [Fact]
    public void Create_should_set_ids()
    {
        var budgetId = Guid.NewGuid();
        var workshopServiceId = Guid.NewGuid();

        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopServiceId);

        Assert.Equal(budgetId, budgetWorkshopService.BudgetId);
        Assert.Equal(workshopServiceId, budgetWorkshopService.WorkshopServiceId);
    }
}
