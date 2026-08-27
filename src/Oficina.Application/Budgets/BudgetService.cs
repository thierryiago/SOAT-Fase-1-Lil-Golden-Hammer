using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.Budgets;

public sealed class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetsRepository;
    private readonly IServiceOrderRepository _serviceOrdersRepository;
    private readonly IPartRepository _partsRepository;
    private readonly IWorkshopServiceRepository _workshopServicesRepository;

    public BudgetService(
        IBudgetRepository budgets,
        IServiceOrderRepository serviceOrders,
        IPartRepository parts,
        IWorkshopServiceRepository workshopServices)
    {
        _budgetsRepository = budgets;
        _serviceOrdersRepository = serviceOrders;
        _partsRepository = parts;
        _workshopServicesRepository = workshopServices;
    }

    public async Task<PagedResponse<BudgetResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var budgets = await _budgetsRepository.ListAsync(cancellationToken);
        var query = budgets
            .OrderByDescending(budget => budget.CreatedAt)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<BudgetResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _budgetsRepository.GetByIdAsync(id, cancellationToken);
        return budget is null ? null : Map(budget);
    }

    public async Task<BudgetResponse> OpenFromServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var existingBudget = await _budgetsRepository.GetByServiceOrderIdAsync(
            serviceOrderId,
            cancellationToken);
        if (existingBudget is not null)
        {
            return Map(existingBudget);
        }

        var serviceOrder = await _serviceOrdersRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service order was not found.");
        }

        if (serviceOrder.WorkshopServices.Count == 0)
        {
            throw new InvalidOperationException(
                "The service order must have at least one workshop service to open a budget.");
        }

        var budgetId = Guid.NewGuid();

        var partIds = serviceOrder.Parts.Select(part => part.PartId).ToList();
        var osParts = await _partsRepository.GetAllById(partIds, cancellationToken);
        var budgetParts = CheckBudgetParts(serviceOrder, osParts, budgetId, partIds);

        var workshopServicesIds = serviceOrder.WorkshopServices.Select(service => service.WorkshopServiceId).ToList();
        var osWorkshopServices = await _workshopServicesRepository.GetAllById(workshopServicesIds, cancellationToken);
        var workshopServices = CheckBudgetWorkshopServices(
            serviceOrder,
            osWorkshopServices,
            budgetId,
            workshopServicesIds);

        var budget = Budget.Open(budgetId, serviceOrder.CustomerId, serviceOrder.Id, budgetParts, workshopServices);
        await _budgetsRepository.AddAsync(budget, cancellationToken);
        return Map(budget);
    }

    private static List<BudgetParts> CheckBudgetParts(
        ServiceOrder serviceOrder,
        List<Part> osParts,
        Guid budgetId,
        List<Guid> partIds)
    {
        var missingPartsIds = partIds
            .Except(osParts.Select(part => part.Id))
            .ToList();
        if (missingPartsIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Parts '{string.Join(", ", missingPartsIds)}' were not found.");
        }

        var partsById = osParts.ToDictionary(part => part.Id);
        var budgetParts = new List<BudgetParts>();
        foreach (var item in serviceOrder.Parts)
        {
            var part = partsById[item.PartId];
            var budgetPart = BudgetParts.Create(
                budgetId,
                item.PartId,
                part.Name,
                part.UnitPrice,
                item.QuantityUsed);
            budgetPart.Part = part;
            budgetParts.Add(budgetPart);
        }
        return budgetParts;
    }

    private static List<BudgetWorkshopServices> CheckBudgetWorkshopServices(
        ServiceOrder serviceOrder,
        List<WorkshopService> osWorkshopServices,
        Guid budgetId,
        List<Guid> workshopServiceIds)
    {
        var missingServiceIds = workshopServiceIds
            .Except(osWorkshopServices.Select(workshopService => workshopService.Id))
            .ToList();
        if (missingServiceIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workshop services '{string.Join(", ", missingServiceIds)}' were not found.");
        }

        var servicesById = osWorkshopServices.ToDictionary(service => service.Id);
        var workshopServices = new List<BudgetWorkshopServices>();
        foreach (var item in serviceOrder.WorkshopServices)
        {
            var workshopService = servicesById[item.WorkshopServiceId];
            var budgetWorkshopService = BudgetWorkshopServices.Create(
                budgetId,
                item.WorkshopServiceId,
                workshopService.Name,
                workshopService.UnitPrice);
            budgetWorkshopService.WorkshopService = workshopService;
            workshopServices.Add(budgetWorkshopService);
        }
        return workshopServices;
    }

    private static BudgetResponse Map(Budget budget) =>
        new(
            budget.Id,
            budget.CustomerId,
            budget.ServiceOrderId,
            budget.CreatedAt,
            budget.IsApproved,
            budget.TotalValue,
            budget.Parts
                .Select(part => new BudgetPartResponse(
                    part.Id,
                    part.PartId,
                    part.PartName,
                    part.Quantity,
                    part.UnitPrice))
                .ToList(),
            budget.WorkshopServices
                .Select(workshopService => new BudgetWorkshopServiceResponse(
                    workshopService.Id,
                    workshopService.WorkshopServiceId,
                    workshopService.WorkshopServiceName,
                    workshopService.UnitPrice))
                .ToList());
}
