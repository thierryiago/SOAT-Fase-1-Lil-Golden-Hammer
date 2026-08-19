using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Services;
using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets;

public sealed class BudgetService
{
    private readonly IBudgetRepository _budgets;
    private readonly IServiceOrderRepository _serviceOrders;
    private readonly IPartRepository _parts;
    private readonly IWorkshopServiceRepository _workshopServices;

    public BudgetService(
        IBudgetRepository budgets,
        IServiceOrderRepository serviceOrders,
        IPartRepository parts,
        IWorkshopServiceRepository workshopServices)
    {
        _budgets = budgets;
        _serviceOrders = serviceOrders;
        _parts = parts;
        _workshopServices = workshopServices;
    }

    public async Task<PagedResponse<BudgetResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var budgets = await _budgets.ListAsync(cancellationToken);
        var query = budgets
            .OrderByDescending(budget => budget.CreatedAt)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<BudgetResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _budgets.GetByIdAsync(id, cancellationToken);
        return budget is null ? null : Map(budget);
    }

    public async Task<BudgetResponse> OpenFromServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.GetByIdAsync(serviceOrderId, cancellationToken);
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

        var parts = new List<BudgetParts>();
        foreach (var item in serviceOrder.Parts)
        {
            var part = item.Part ?? await _parts.GetByIdAsync(item.PartId, cancellationToken);
            if (part is null)
            {
                throw new InvalidOperationException($"Part '{item.PartId}' was not found.");
            }

            var budgetPart = BudgetParts.Create(budgetId, item.PartId, item.QuantityUsed);
            budgetPart.Part = part;
            parts.Add(budgetPart);
        }

        var workshopServices = new List<BudgetWorkshopServices>();
        foreach (var item in serviceOrder.WorkshopServices)
        {
            var workshopService = item.WorkshopService ?? await _workshopServices.GetByIdAsync(item.WorkshopServiceId, cancellationToken);
            if (workshopService is null)
            {
                throw new InvalidOperationException($"Workshop service '{item.WorkshopServiceId}' was not found.");
            }

            var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, item.WorkshopServiceId);
            budgetWorkshopService.WorkshopService = workshopService;
            workshopServices.Add(budgetWorkshopService);
        }

        var budget = Budget.Open(budgetId, serviceOrder.CustomerId, serviceOrder.Id, parts, workshopServices);
        await _budgets.AddAsync(budget, cancellationToken);
        return Map(budget);
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
