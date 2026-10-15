using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using System.Globalization;
using System.Text;

namespace Oficina.Infrastructure.Notifications;

public sealed class SendBudgetAwaitingApproval(
    IEmailTransport emailSender)
{
    private const string ApproveBudgetUrl = "https://localhost:5001/api/v1/notifications/approveBudget";
    private const string RejectBudgetUrl = "https://localhost:5001/api/v1/notifications/rejectBudget";

    private readonly IEmailTransport _emailSender = emailSender;

    public Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null)
    {
        var subject = $"{customerName} - Orçamento aguardando aprovação";
        var body = BuildBudgetBody(budget, context);

        return _emailSender.SendAsync(customerEmail, subject, body, isHtml: true, cancellationToken);
    }

    private static string BuildBudgetBody(BudgetResponse budget, ServiceOrderStatusEmail? context)
    {
        var body = new StringBuilder()
            .AppendLine("<html><body>")
            .AppendLine("<h2>Orçamento aguardando aprovação</h2>")
            .AppendLine(context is null ? string.Empty : $"<p>{System.Net.WebUtility.HtmlEncode(SendServiceOrderStatus.BuildBody(context)).Replace("\n", "<br>")}</p>")
            .AppendLine($"<p><strong>Orçamento ID:</strong> {budget.Id}</p>")
            .AppendLine($"<p><strong>Ordem de serviço ID:</strong> {budget.ServiceOrderId}</p>")
            .AppendLine($"<p><strong>Criado em:</strong> {budget.CreatedAt:O}</p>")
            .AppendLine("<h3>Peças:</h3>")
            .AppendLine("<ul>");

        if (budget.Parts.Count == 0)
        {
            body.AppendLine("<li>Nenhum</li>");
        }
        else
        {
            foreach (var part in budget.Parts)
            {
                var itemTotal = part.UnitPrice * part.Quantity;
                body.AppendLine(
                    $"<li>{System.Net.WebUtility.HtmlEncode(part.PartName)} | Quantidade: {part.Quantity} | Preço unitário: {FormatMoney(part.UnitPrice)} | Total: {FormatMoney(itemTotal)}</li>");
            }
        }

        body.AppendLine("</ul>")
            .AppendLine("<h3>Serviços:</h3>")
            .AppendLine("<ul>");

        if (budget.WorkshopServices.Count == 0)
        {
            body.AppendLine("<li>Nenhum</li>");
        }
        else
        {
            foreach (var service in budget.WorkshopServices)
            {
                body.AppendLine(
                    $"<li>{System.Net.WebUtility.HtmlEncode(service.WorkshopServiceName)} | Preço unitário: {FormatMoney(service.UnitPrice)}</li>");
            }
        }

        body.AppendLine("</ul>")
            .AppendLine($"<p><strong>Valor total:</strong> {FormatMoney(budget.TotalValue)}</p>")
            .AppendLine(
                $"<p><a href=\"{ApproveBudgetUrl}?budgetId={budget.Id}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Aprovar orçamento</a>")
            .AppendLine(
                $"<a href=\"{RejectBudgetUrl}?budgetId={budget.Id}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Rejeitar orçamento</a></p>")
            .Append("</body></html>");

        return body.ToString();
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
