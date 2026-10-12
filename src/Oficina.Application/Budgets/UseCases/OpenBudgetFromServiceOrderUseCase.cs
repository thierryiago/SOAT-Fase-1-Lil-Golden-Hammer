using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets.UseCases;

public sealed class OpenBudgetFromServiceOrderUseCase(
    IBudgetRepository budgets,
    IServiceOrderRepository serviceOrders,
    IPartRepository parts,
    IWorkshopServiceRepository workshopServices,
    BudgetFactory budgetFactory)
{
    public async Task<BudgetResponse> OpenFromServiceOrderAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await serviceOrders.GetByIdAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Service order was not found.");

        var budgetId = Guid.NewGuid();
        var partIds = serviceOrder.Parts.Select(part => part.PartId).ToList();
        var catalogParts = await parts.GetAllById(partIds, cancellationToken);
        var workshopServiceIds = serviceOrder.WorkshopServices.Select(service => service.WorkshopServiceId).ToList();
        var catalogWorkshopServices = await workshopServices.GetAllById(workshopServiceIds, cancellationToken);

        var budget = budgetFactory.Create(budgetId, serviceOrder, catalogParts, catalogWorkshopServices);
        await budgets.AddAsync(budget, cancellationToken);
        return BudgetResponseMapper.Map(budget);
    }
}
