using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Domain.Budget;

public sealed class BudgetFactory
{
    public Budget Create(
        Guid budgetId,
        ServiceOrder serviceOrder,
        IReadOnlyCollection<Part> catalogParts,
        IReadOnlyCollection<WorkshopService> catalogWorkshopServices)
    {
        ArgumentNullException.ThrowIfNull(serviceOrder);
        ArgumentNullException.ThrowIfNull(catalogParts);
        ArgumentNullException.ThrowIfNull(catalogWorkshopServices);

        if (serviceOrder.WorkshopServices.Count == 0)
        {
            throw new InvalidOperationException(
                "The service order must have at least one workshop service to open a budget.");
        }

        var missingPartIds = serviceOrder.Parts.Select(part => part.PartId)
            .Except(catalogParts.Select(part => part.Id))
            .ToList();
        if (missingPartIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Parts '{string.Join(", ", missingPartIds)}' were not found.");
        }

        var missingServiceIds = serviceOrder.WorkshopServices.Select(service => service.WorkshopServiceId)
            .Except(catalogWorkshopServices.Select(service => service.Id))
            .ToList();
        if (missingServiceIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workshop services '{string.Join(", ", missingServiceIds)}' were not found.");
        }

        var partsById = catalogParts.ToDictionary(part => part.Id);
        var budgetParts = new List<BudgetParts>();
        foreach (var orderPart in serviceOrder.Parts)
        {
            var part = partsById[orderPart.PartId];
            var budgetPart = BudgetParts.Create(
                budgetId, part.Id, part.Name, part.UnitPrice, orderPart.QuantityUsed);
            budgetPart.Part = part;
            budgetParts.Add(budgetPart);
        }

        var servicesById = catalogWorkshopServices.ToDictionary(service => service.Id);
        var budgetWorkshopServices = new List<BudgetWorkshopServices>();
        foreach (var orderService in serviceOrder.WorkshopServices)
        {
            var workshopService = servicesById[orderService.WorkshopServiceId];
            var budgetWorkshopService = BudgetWorkshopServices.Create(
                budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
            budgetWorkshopService.WorkshopService = workshopService;
            budgetWorkshopServices.Add(budgetWorkshopService);
        }

        return Budget.Open(
            budgetId, serviceOrder.CustomerId, serviceOrder.Id, budgetParts, budgetWorkshopServices);
    }
}
