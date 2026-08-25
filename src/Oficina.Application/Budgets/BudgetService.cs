using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.Budgets;

public sealed class BudgetService
{
    private readonly IBudgetRepository _budgetsRepository;
    private readonly IServiceOrderRepository _serviceOrdersRepositoryRepository;
    private readonly IPartRepository _partsRepository;
    private readonly IWorkshopServiceRepository _workshopServicesRepository;

    public BudgetService(
        IBudgetRepository budgets,
        IServiceOrderRepository serviceOrders,
        IPartRepository parts,
        IWorkshopServiceRepository workshopServices)
    {
        _budgetsRepository = budgets;
        _serviceOrdersRepositoryRepository = serviceOrders;
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
        var serviceOrder = await _serviceOrdersRepositoryRepository.GetByIdAsync(serviceOrderId, cancellationToken);
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

        var partIds = serviceOrder.Parts.Select(part => part.Id).ToList();
        var osParts = await _partsRepository.GetAllById(partIds, cancellationToken);
        var budgetParts = CheckBudgetParts(serviceOrder, osParts, budgetId, partIds, cancellationToken);

        var workshopServicesIds = serviceOrder.WorkshopServices.Select(service => service.Id).ToList();
        var osWorkshopServices = await _workshopServicesRepository.GetAllById(workshopServicesIds, cancellationToken);
        var workshopServices = CheckBudgetWorkShopService(serviceOrder, osWorkshopServices, budgetId, workshopServicesIds, cancellationToken);

        var budget = Budget.Open(budgetId, serviceOrder.CustomerId, serviceOrder.Id, budgetParts, workshopServices);
        await _budgetsRepository.AddAsync(budget, cancellationToken);
        return Map(budget);
    }

    private  List<BudgetParts> CheckBudgetParts(ServiceOrder serviceOrder, List<Part> osParts,
        Guid budgetId, List<Guid> partIds, CancellationToken cancellationToken)
    {
        var missingPartsIds = partIds
            .Except(osParts.Select(part => part.Id))
            .ToList();
        if (missingPartsIds is null)
        {
            throw new InvalidOperationException($"Parts was not found.");
        }
        if (missingPartsIds.Count > 0)
        {
            throw new InvalidOperationException($"Part '{missingPartsIds.ToString()}' was not found.");
        }

        var budgetParts = new List<BudgetParts>();
        foreach (var item in serviceOrder.Parts)
        {
            var budgetPart = BudgetParts.Create(budgetId, item.PartId, item.QuantityUsed);
            budgetPart.Part = item.Part;
            budgetParts.Add(budgetPart);
        }
        return budgetParts;
    }

    private List<BudgetWorkshopServices> CheckBudgetWorkShopService(ServiceOrder serviceOrder, List<WorkshopService> OsWorkshopServices,
        Guid budgetId, List<Guid> WorkshopServicesIds, CancellationToken cancellationToken)
    {
        var missingServiceIds = WorkshopServicesIds
            .Except(OsWorkshopServices.Select(workshopService => workshopService.Id))
            .ToList();
        if (missingServiceIds is null)
        {
            throw new InvalidOperationException($"Parts was not found.");
        }
        if (missingServiceIds.Count > 0)
        {
            throw new InvalidOperationException($"Part '{missingServiceIds.ToString()}' was not found.");
        }

        var workshopServices = new List<BudgetWorkshopServices>();
        foreach (var item in serviceOrder.WorkshopServices)
        {
            var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, item.WorkshopServiceId);
            budgetWorkshopService.WorkshopService = item.WorkshopService;
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
                    part.Part?.Name ?? string.Empty,
                    part.Quantity,
                    part.Part?.UnitPrice ?? 0))
                .ToList(),
            budget.WorkshopServices
                .Select(workshopService => new BudgetWorkshopServiceResponse(
                    workshopService.Id,
                    workshopService.WorkshopServiceId,
                    workshopService.WorkshopService?.Name ?? string.Empty,
                    workshopService.WorkshopService?.UnitPrice ?? 0))
                .ToList());
}
