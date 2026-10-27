using Microsoft.Extensions.Options;

namespace Oficina.Infrastructure.Notifications;

public sealed class BudgetDecisionLinks(IOptions<AppOptions> options)
{
    private readonly string _publicBaseUrl = new Uri(options.Value.PublicBaseUrl)
        .GetLeftPart(UriPartial.Authority);

    public string Approve(Guid budgetId) =>
        $"{_publicBaseUrl}/api/v1/notifications/approveBudget?budgetId={budgetId}";

    public string Reject(Guid budgetId) =>
        $"{_publicBaseUrl}/api/v1/notifications/rejectBudget?budgetId={budgetId}";
}
