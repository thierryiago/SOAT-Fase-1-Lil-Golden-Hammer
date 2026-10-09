using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets;

public sealed class BudgetService(
    IBudgetRepository budgets,
    IServiceOrderRepository serviceOrders,
    IPartRepository parts,
    IWorkshopServiceRepository workshopServices,
    BudgetFactory budgetFactory) : IBudgetService
{
    private readonly IBudgetRepository _budgetsRepository = budgets;
    private readonly IServiceOrderRepository _serviceOrdersRepository = serviceOrders;
    private readonly IPartRepository _partsRepository = parts;
    private readonly IWorkshopServiceRepository _workshopServicesRepository = workshopServices;
    private readonly BudgetFactory _budgetFactory = budgetFactory;

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
        var serviceOrder = await _serviceOrdersRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service order was not found.");
        }

        var budgetId = Guid.NewGuid();
        var partIds = serviceOrder.Parts.Select(part => part.PartId).ToList();
        var catalogParts = await _partsRepository.GetAllById(partIds, cancellationToken);
        var workshopServiceIds = serviceOrder.WorkshopServices.Select(service => service.WorkshopServiceId).ToList();
        var catalogWorkshopServices = await _workshopServicesRepository.GetAllById(workshopServiceIds, cancellationToken);

        var budget = _budgetFactory.Create(budgetId, serviceOrder, catalogParts, catalogWorkshopServices);
        await _budgetsRepository.AddAsync(budget, cancellationToken);
        return Map(budget);
    }

    public async Task SetApprovalByServiceOrderAsync(
        Guid serviceOrderId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        var budget = await _budgetsRepository.GetByServiceOrderIdAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Budget was not found for the service order.");

        if (budget.IsApproved.HasValue)
        {
            return;
        }

        budget.SetApproval(isApproved);
        await _budgetsRepository.UpdateAsync(budget, cancellationToken);
    }

    public async Task<Budget> SetApprovalByBudgetIdAsync(Guid budgetId, bool isApproved, CancellationToken cancellationToken)
    {
        var budget = await _budgetsRepository.GetByIdAsync(budgetId, cancellationToken)
            ?? throw new InvalidOperationException("Budget was not found.");

        if (budget.IsApproved.HasValue)
        {
            if (budget.IsApproved.Value)
                throw new InvalidOperationException("Budget is already approved.");
            else
                if (!budget.IsApproved.Value)
                    throw new InvalidOperationException("Budget is already rejected.");
        }

        budget.SetApproval(isApproved);
        await _budgetsRepository.UpdateAsync(budget, cancellationToken);

        return budget;
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
