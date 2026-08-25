using Oficina.Application.Metrics;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Application;

public sealed class MetricsServiceTests
{
    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_distribute_order_duration_by_estimated_time()
    {
        var oilChangeId = Guid.NewGuid();
        var alignmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
        var service = CreateService(
            Services((oilChangeId, "Oil change", 60), (alignmentId, "Wheel alignment", 120)),
            [
                Order(
                    orderId,
                    [(oilChangeId, 60), (alignmentId, 120)],
                    [
                        History(orderId, ServiceOrderStatus.InExecution, baseDate),
                        History(orderId, ServiceOrderStatus.Finalized, baseDate.AddMinutes(180))
                    ])
            ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        Assert.Equal(60m, response.Single(metric => metric.Id == oilChangeId).AverageTimeMinutes);
        Assert.Equal(120m, response.Single(metric => metric.Id == alignmentId).AverageTimeMinutes);
    }

    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_average_allocated_durations_for_the_same_service()
    {
        var workshopServiceId = Guid.NewGuid();
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
        var service = CreateService(
            Services((workshopServiceId, "Oil change", 60)),
            [
                Order(
                    firstOrderId,
                    [(workshopServiceId, 60)],
                    [
                        History(firstOrderId, ServiceOrderStatus.InExecution, baseDate),
                        History(firstOrderId, ServiceOrderStatus.Finalized, baseDate.AddMinutes(60))
                    ]),
                Order(
                    secondOrderId,
                    [(workshopServiceId, 60)],
                    [
                        History(secondOrderId, ServiceOrderStatus.InExecution, baseDate.AddHours(2)),
                        History(secondOrderId, ServiceOrderStatus.Finalized, baseDate.AddHours(4))
                    ])
            ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        Assert.Equal(90m, Assert.Single(response).AverageTimeMinutes);
    }

    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_consolidate_repeated_services_in_an_order()
    {
        var oilChangeId = Guid.NewGuid();
        var alignmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
        var service = CreateService(
            Services((oilChangeId, "Oil change", 30), (alignmentId, "Wheel alignment", 120)),
            [
                Order(
                    orderId,
                    [(oilChangeId, 30), (oilChangeId, 30), (alignmentId, 120)],
                    [
                        History(orderId, ServiceOrderStatus.InExecution, baseDate),
                        History(orderId, ServiceOrderStatus.Finalized, baseDate.AddMinutes(180))
                    ])
            ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        Assert.Equal(60m, response.Single(metric => metric.Id == oilChangeId).AverageTimeMinutes);
        Assert.Equal(120m, response.Single(metric => metric.Id == alignmentId).AverageTimeMinutes);
    }

    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_keep_services_without_valid_executions()
    {
        var workshopServiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = CreateService(
            Services((workshopServiceId, "Wheel alignment", 45)),
            [
                Order(
                    orderId,
                    [(workshopServiceId, 45)],
                    [History(orderId, ServiceOrderStatus.Finalized, DateTime.UtcNow)])
            ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        Assert.Null(Assert.Single(response).AverageTimeMinutes);
    }

    private static IReadOnlyCollection<WorkshopServiceExecutionTimeData> Services(
        params (Guid Id, string Name, int EstimatedDurationMinutes)[] services) =>
        services
            .Select(service => new WorkshopServiceExecutionTimeData(
                service.Id,
                service.Name,
                service.EstimatedDurationMinutes))
            .ToList();

    private static ServiceOrderExecutionTimeData Order(
        Guid orderId,
        IReadOnlyCollection<(Guid WorkshopServiceId, int EstimatedDurationMinutes)> workshopServices,
        IReadOnlyCollection<ServiceOrderStatusHistoryData> histories) =>
        new(
            orderId,
            workshopServices
                .Select(service => new ServiceOrderWorkshopServiceData(
                    service.WorkshopServiceId,
                    service.EstimatedDurationMinutes))
                .ToList(),
            histories);

    private static MetricsService CreateService(
        IReadOnlyCollection<WorkshopServiceExecutionTimeData> workshopServices,
        IReadOnlyCollection<ServiceOrderExecutionTimeData> serviceOrders) =>
        new(new FakeWorkshopServiceExecutionTimeRepository(
            new WorkshopServiceExecutionTimesData(workshopServices, serviceOrders)));

    private static ServiceOrderStatusHistoryData History(
        Guid serviceOrderId,
        ServiceOrderStatus status,
        DateTime createdDate) =>
        new(serviceOrderId, status.ToString(), createdDate);

    private sealed class FakeWorkshopServiceExecutionTimeRepository(
        WorkshopServiceExecutionTimesData data)
        : IMetricExecutionTimeRepository
    {
        public Task<WorkshopServiceExecutionTimesData> GetAsync(
            CancellationToken cancellationToken) => Task.FromResult(data);
    }
}
