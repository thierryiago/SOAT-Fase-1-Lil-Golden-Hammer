namespace Oficina.Application.Metrics;

public sealed record WorkshopServiceExecutionTimeResponse(
    Guid Id,
    string Name,
    int EstimatedTimeMinutes,
    decimal? AverageTimeMinutes);

public sealed record WorkshopServiceExecutionTimeData(
    Guid WorkshopServiceId,
    string WorkshopServiceName,
    int WorkshopEstimatedDurationMinutes,
    IReadOnlyCollection<ServiceOrderStatusHistoryData> Histories);

public sealed record ServiceOrderStatusHistoryData(
    Guid ServiceOrderId,
    string? StatusName,
    DateTime CreatedDate);
