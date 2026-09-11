using Oficina.Application.Budgets;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace Oficina.Application.Notifications;

public sealed class NotificationService
{
    private const string Subject = "Notificação da Oficina";
    private const string Body = "Esta é uma notificação enviada pela Oficina.";
    private const string ApproveBudgetUrl = "https://localhost:5001/api/v1/notifications/approveBudget";
    private const string RejectBudgetUrl = "https://localhost:5001/api/v1/notifications/rejectBudget";

    private readonly INotificationEmailSender _emailSender;

    public NotificationService(INotificationEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email is invalid.");
        }

        return _emailSender.SendAsync(email, Subject, Body, isHtml: false, cancellationToken);
    }

    public Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken)
    {
        var subject = $"{customerName} - Budget Awaiting to Approval";
        var body = BuildBudgetBody(budget);

        return _emailSender.SendAsync(customerEmail, subject, body, isHtml: true, cancellationToken);
    }

    public Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken)
    {
        const string subject = "Vehicle ready for pickup";
        var body = new StringBuilder()
            .AppendLine($"Hello, {customerName}!")
            .AppendLine()
            .AppendLine("Your vehicle is ready to be picked up at the workshop.")
            .AppendLine()
            .AppendLine("Vehicle details:")
            .AppendLine($"- Plate: {vehiclePlate}")
            .AppendLine($"- Brand: {vehicleBrand}")
            .AppendLine($"- Model: {vehicleModel}")
            .Append($"- Year: {vehicleYear}")
            .ToString();

        return _emailSender.SendAsync(customerEmail, subject, body, isHtml: false, cancellationToken);
    }

    private static string BuildBudgetBody(BudgetResponse budget)
    {
        var body = new StringBuilder()
            .AppendLine("<html><body>")
            .AppendLine("<h2>Budget awaiting approval</h2>")
            .AppendLine($"<p><strong>Budget ID:</strong> {budget.Id}</p>")
            .AppendLine($"<p><strong>Service Order ID:</strong> {budget.ServiceOrderId}</p>")
            .AppendLine($"<p><strong>Created At:</strong> {budget.CreatedAt:O}</p>")
            .AppendLine("<h3>Parts:</h3>")
            .AppendLine("<ul>");

        if (budget.Parts.Count == 0)
        {
            body.AppendLine("<li>None</li>");
        }
        else
        {
            foreach (var part in budget.Parts)
            {
                var itemTotal = part.UnitPrice * part.Quantity;
                body.AppendLine(
                    $"<li>{part.PartName} | Quantity: {part.Quantity} | Unit Price: {FormatMoney(part.UnitPrice)} | Total: {FormatMoney(itemTotal)}</li>");
            }
        }

        body.AppendLine("</ul>")
            .AppendLine("<h3>Workshop Services:</h3>")
            .AppendLine("<ul>");

        if (budget.WorkshopServices.Count == 0)
        {
            body.AppendLine("<li>None</li>");
        }
        else
        {
            foreach (var service in budget.WorkshopServices)
            {
                body.AppendLine(
                    $"<li>{service.WorkshopServiceName} | Unit Price: {FormatMoney(service.UnitPrice)}</li>");
            }
        }

        body.AppendLine("</ul>")
            .AppendLine($"<p><strong>Total Value:</strong> {FormatMoney(budget.TotalValue)}</p>")
            .AppendLine(
                $"<p><a href=\"{ApproveBudgetUrl}?budgetId={budget.Id}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Approve Budget</a>")
            .AppendLine(
                $"<a href=\"{RejectBudgetUrl}?budgetId={budget.Id}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Reject Budget</a></p>")
            .Append("</body></html>");

        return body.ToString();
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
