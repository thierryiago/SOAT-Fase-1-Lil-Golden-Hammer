using Oficina.Domain.Budget;
using Oficina.Domain.Parts;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Domain;

public sealed class BudgetTests
{
    [Fact]
    public void Open_should_reject_empty_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { CreateWorkshopServiceItem() };

        var act = () => Budget.Open(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, workshopServices);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_empty_customer_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { CreateWorkshopServiceItem() };

        var act = () => Budget.Open(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, workshopServices);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Open_should_reject_empty_service_order_id()
    {
        var workshopServices = new List<BudgetWorkshopServices> { CreateWorkshopServiceItem() };

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
        var budgetPart = BudgetParts.Create(budgetId, part.Id, part.Name, part.UnitPrice, 3);
        budgetPart.Part = part;

        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
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
        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
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

    // Item 22 of docs/analise-gaps-e-cenarios-faltantes.md: an explicit, non-null empty parts
    // list (new List<BudgetParts>()) must behave exactly like passing null - same TotalValue,
    // same empty collection - guarding against a bug where only one of the two shapes is handled.
    [Fact]
    public void Open_should_calculate_the_same_total_value_for_null_and_empty_parts_list()
    {
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);

        var budgetIdForNull = Guid.NewGuid();
        var budgetWorkshopServiceForNull = BudgetWorkshopServices.Create(
            budgetIdForNull, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
        budgetWorkshopServiceForNull.WorkshopService = workshopService;
        var budgetWithNullParts = Budget.Open(
            budgetIdForNull, Guid.NewGuid(), Guid.NewGuid(), null,
            new List<BudgetWorkshopServices> { budgetWorkshopServiceForNull });

        var budgetIdForEmpty = Guid.NewGuid();
        var budgetWorkshopServiceForEmpty = BudgetWorkshopServices.Create(
            budgetIdForEmpty, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
        budgetWorkshopServiceForEmpty.WorkshopService = workshopService;
        var budgetWithEmptyParts = Budget.Open(
            budgetIdForEmpty, Guid.NewGuid(), Guid.NewGuid(), new List<BudgetParts>(),
            new List<BudgetWorkshopServices> { budgetWorkshopServiceForEmpty });

        Assert.Equal(budgetWithNullParts.TotalValue, budgetWithEmptyParts.TotalValue);
        Assert.Empty(budgetWithNullParts.Parts);
        Assert.Empty(budgetWithEmptyParts.Parts);
    }

    private static BudgetWorkshopServices CreateWorkshopServiceItem() =>
        BudgetWorkshopServices.Create(Guid.NewGuid(), Guid.NewGuid(), "Servico", 100m);
}

public sealed class BudgetPartsTests
{
    [Fact]
    public void Create_should_reject_non_positive_quantity()
    {
        var act = () => BudgetParts.Create(Guid.NewGuid(), Guid.NewGuid(), "Filtro", 10m, 0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_set_ids_and_quantity()
    {
        var budgetId = Guid.NewGuid();
        var partId = Guid.NewGuid();

        var budgetPart = BudgetParts.Create(budgetId, partId, "Filtro", 10m, 5);

        Assert.Equal(budgetId, budgetPart.BudgetId);
        Assert.Equal(partId, budgetPart.PartId);
        Assert.Equal("Filtro", budgetPart.PartName);
        Assert.Equal(10m, budgetPart.UnitPrice);
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

        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopServiceId, "Troca de oleo", 100m);

        Assert.Equal(budgetId, budgetWorkshopService.BudgetId);
        Assert.Equal(workshopServiceId, budgetWorkshopService.WorkshopServiceId);
        Assert.Equal("Troca de oleo", budgetWorkshopService.WorkshopServiceName);
        Assert.Equal(100m, budgetWorkshopService.UnitPrice);
    }
}
