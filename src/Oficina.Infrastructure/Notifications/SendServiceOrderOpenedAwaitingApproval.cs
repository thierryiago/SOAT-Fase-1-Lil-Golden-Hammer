using Oficina.Application.Notifications;
using System.Globalization;
using System.Net;
using System.Text;

namespace Oficina.Infrastructure.Notifications;

public sealed class SendServiceOrderOpenedAwaitingApproval(
    IEmailTransport emailSender,
    BudgetDecisionLinks decisionLinks)
{
    private readonly IEmailTransport _emailSender = emailSender;

    public Task SendAsync(ServiceOrderOpenedEmail email, CancellationToken cancellationToken)
    {
        var subject = $"{email.CustomerName} - OS aberta: orçamento aguardando aprovação";
        var body = BuildBody(email);

        return _emailSender.SendAsync(email.CustomerEmail, subject, body, isHtml: true, cancellationToken);
    }

    private string BuildBody(ServiceOrderOpenedEmail email)
    {
        var budget = email.Budget;
        var body = new StringBuilder()
            .AppendLine("<html><body>")
            .AppendLine($"<h2>Olá, {Encode(email.CustomerName)}!</h2>")
            .AppendLine("<p>Sua ordem de serviço foi aberta e o orçamento está aguardando a sua aprovação.</p>")
            .AppendLine("<h3>Ordem de serviço</h3>")
            .AppendLine("<ul>")
            .AppendLine($"<li><strong>ID:</strong> {email.ServiceOrderId}</li>")
            .AppendLine($"<li><strong>Descrição:</strong> {Encode(email.Description)}</li>")
            .AppendLine($"<li><strong>Aberta em:</strong> {email.CreatedAt:O}</li>")
            .AppendLine($"<li><strong>Orçamento ID:</strong> {budget.Id}</li>")
            .AppendLine("</ul>")
            .AppendLine("<h3>Cliente</h3>")
            .AppendLine("<ul>")
            .AppendLine($"<li><strong>Nome:</strong> {Encode(email.CustomerName)}</li>")
            .AppendLine($"<li><strong>Documento:</strong> {Encode(email.CustomerDocument)}</li>")
            .AppendLine($"<li><strong>E-mail:</strong> {Encode(email.CustomerEmail)}</li>")
            .AppendLine($"<li><strong>Telefone:</strong> {Encode(email.CustomerTelephoneNumber)}</li>")
            .AppendLine("</ul>")
            .AppendLine("<h3>Veículo</h3>")
            .AppendLine("<ul>")
            .AppendLine($"<li><strong>Placa:</strong> {Encode(email.VehiclePlate)}</li>")
            .AppendLine($"<li><strong>Marca:</strong> {Encode(email.VehicleBrand)}</li>")
            .AppendLine($"<li><strong>Modelo:</strong> {Encode(email.VehicleModel)}</li>")
            .AppendLine($"<li><strong>Ano:</strong> {email.VehicleYear}</li>")
            .AppendLine("</ul>")
            .AppendLine("<h3>Serviços</h3>")
            .AppendLine("<ul>");

        foreach (var service in budget.WorkshopServices)
        {
            body.AppendLine(
                $"<li>{Encode(service.WorkshopServiceName)} | Preço unitário: {FormatMoney(service.UnitPrice)}</li>");
        }

        body.AppendLine("</ul>")
            .AppendLine("<h3>Peças</h3>")
            .AppendLine("<ul>");

        if (budget.Parts.Count == 0)
        {
            body.AppendLine("<li>Nenhuma</li>");
        }
        else
        {
            foreach (var part in budget.Parts)
            {
                var itemTotal = part.UnitPrice * part.Quantity;
                body.AppendLine(
                    $"<li>{Encode(part.PartName)} | Quantidade: {part.Quantity} | Preço unitário: {FormatMoney(part.UnitPrice)} | Total: {FormatMoney(itemTotal)}</li>");
            }
        }

        body.AppendLine("</ul>")
            .AppendLine($"<p><strong>Valor total:</strong> {FormatMoney(budget.TotalValue)}</p>")
            .AppendLine(
                $"<p><a href=\"{decisionLinks.Approve(budget.Id)}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Aprovar orçamento</a>")
            .AppendLine(
                $"<a href=\"{decisionLinks.Reject(budget.Id)}\" " +
                "style=\"display:inline-block;padding:12px 24px;background-color:#1a73e8;color:#ffffff;" +
                "text-decoration:none;border-radius:4px;font-weight:bold;\">Rejeitar orçamento</a></p>")
            .Append("</body></html>");

        return body.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
