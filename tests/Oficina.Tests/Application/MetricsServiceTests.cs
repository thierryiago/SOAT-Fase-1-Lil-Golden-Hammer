using Oficina.Application.Metrics;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Application;

public sealed class MetricsServiceTests
{
    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_calculate_the_simple_average()
    {
        var workshopServiceId = Guid.NewGuid();
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
        var service = CreateService(
        [
            new WorkshopServiceExecutionTimeData(
                workshopServiceId,
                "Oil change",
                60,
                [
                    History(firstOrderId, ServiceOrderStatus.InExecution, baseDate),
                    History(firstOrderId, ServiceOrderStatus.Finalized, baseDate.AddMinutes(60)),
                    History(secondOrderId, ServiceOrderStatus.InExecution, baseDate.AddHours(2)),
                    History(secondOrderId, ServiceOrderStatus.InExecution, baseDate.AddHours(3)),
                    History(secondOrderId, ServiceOrderStatus.Finalized, baseDate.AddHours(5))
                ])
        ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        var metric = Assert.Single(response);
        Assert.Equal(workshopServiceId, metric.Id);
        Assert.Equal("Oil change", metric.Name);
        Assert.Equal(60, metric.EstimatedTimeMinutes);
        Assert.Equal(90m, metric.AverageTimeMinutes);
    }

    [Fact]
    public async Task GetWorkshopServiceExecutionTimesAsync_should_keep_services_without_valid_executions()
    {
        var service = CreateService(
        [
            new WorkshopServiceExecutionTimeData(
                Guid.NewGuid(),
                "Wheel alignment",
                45,
                [History(Guid.NewGuid(), ServiceOrderStatus.Finalized, DateTime.UtcNow)])
        ]);

        var response = await service.GetWorkshopServiceExecutionTimesAsync(CancellationToken.None);

        Assert.Null(Assert.Single(response).AverageTimeMinutes);
    }

    private static MetricsService CreateService(IReadOnlyCollection<WorkshopServiceExecutionTimeData> data) =>
        new(new FakeWorkshopServiceExecutionTimeRepository(data));

    private static ServiceOrderStatusHistoryData History(
        Guid serviceOrderId,
        ServiceOrderStatus status,
        DateTime createdDate) =>
        new(serviceOrderId, status.ToString(), createdDate);

    private sealed class FakeWorkshopServiceExecutionTimeRepository(
        IReadOnlyCollection<WorkshopServiceExecutionTimeData> data)
        : IWorkshopServiceExecutionTimeRepository
    {
        public Task<IReadOnlyCollection<WorkshopServiceExecutionTimeData>> ListAsync(
            CancellationToken cancellationToken) => Task.FromResult(data);
    }
}
