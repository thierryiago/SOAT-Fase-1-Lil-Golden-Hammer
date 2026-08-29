using Oficina.Application.Budgets;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace Oficina.Application.Notifications;

public sealed class NotificationService
{
    private const string Subject = "Notificação da Oficina";
    private const string Body = "Esta é uma notificação enviada pela Oficina.";

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

        return _emailSender.SendAsync(email, Subject, Body, cancellationToken);
    }

    public Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken)
    {
        var subject = $"{customerName} - Budget Awaiting to Approval";
        var body = BuildBudgetBody(budget);

        return _emailSender.SendAsync(customerEmail, subject, body, cancellationToken);
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

        return _emailSender.SendAsync(customerEmail, subject, body, cancellationToken);
    }

    private static string BuildBudgetBody(BudgetResponse budget)
    {
        var body = new StringBuilder()
            .AppendLine($"Budget ID: {budget.Id}")
            .AppendLine($"Service Order ID: {budget.ServiceOrderId}")
            .AppendLine($"Created At: {budget.CreatedAt:O}")
            .AppendLine()
            .AppendLine("Parts:");

        if (budget.Parts.Count == 0)
        {
            body.AppendLine("- None");
        }
        else
        {
            foreach (var part in budget.Parts)
            {
                var itemTotal = part.UnitPrice * part.Quantity;
                body.AppendLine(
                    $"- {part.PartName} | Quantity: {part.Quantity} | Unit Price: {FormatMoney(part.UnitPrice)} | Total: {FormatMoney(itemTotal)}");
            }
        }

        body.AppendLine()
            .AppendLine("Workshop Services:");

        if (budget.WorkshopServices.Count == 0)
        {
            body.AppendLine("- None");
        }
        else
        {
            foreach (var service in budget.WorkshopServices)
            {
                body.AppendLine(
                    $"- {service.WorkshopServiceName} | Unit Price: {FormatMoney(service.UnitPrice)}");
            }
        }

        body.AppendLine()
            .Append($"Total Value: {FormatMoney(budget.TotalValue)}");

        return body.ToString();
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
