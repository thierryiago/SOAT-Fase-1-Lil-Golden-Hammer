namespace Oficina.Application.Metrics;

public sealed record WorkshopServiceExecutionTimeResponse(
    Guid Id,
    string Name,
    int EstimatedTimeMinutes,
    decimal? AverageTimeMinutes);

public sealed record WorkshopServiceExecutionTimesData(
    IReadOnlyCollection<WorkshopServiceExecutionTimeData> WorkshopServices,
    IReadOnlyCollection<ServiceOrderExecutionTimeData> ServiceOrders);

public sealed record WorkshopServiceExecutionTimeData(
    Guid WorkshopServiceId,
    string WorkshopServiceName,
    int WorkshopEstimatedDurationMinutes);

public sealed record ServiceOrderExecutionTimeData(
    Guid ServiceOrderId,
    IReadOnlyCollection<ServiceOrderWorkshopServiceData> WorkshopServices,
    IReadOnlyCollection<ServiceOrderStatusHistoryData> Histories);

public sealed record ServiceOrderWorkshopServiceData(
    Guid WorkshopServiceId,
    int EstimatedDurationMinutes);

public sealed record ServiceOrderStatusHistoryData(
    Guid ServiceOrderId,
    string? StatusName,
    DateTime CreatedDate);
