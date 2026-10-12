using Oficina.Application.Notifications;
using System.Text;

namespace Oficina.Infrastructure.Notifications;

public sealed class SendVehicleReadyForPickup(
    IEmailTransport emailSender)
{
    private readonly IEmailTransport _emailSender = emailSender;

    public Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null)
    {
        const string subject = "Veículo pronto para retirada";
        var body = new StringBuilder()
            .AppendLine($"Olá, {customerName}!")
            .AppendLine()
            .AppendLine("Seu veículo está pronto para retirada na oficina.")
            .AppendLine()
            .AppendLine("Dados do veículo:")
            .AppendLine($"- Placa: {vehiclePlate}")
            .AppendLine($"- Marca: {vehicleBrand}")
            .AppendLine($"- Modelo: {vehicleModel}")
            .Append($"- Ano: {vehicleYear}")
            .ToString();

        if (context is not null)
            body = SendServiceOrderStatus.BuildBody(context);

        return _emailSender.SendAsync(customerEmail, subject, body, isHtml: false, cancellationToken);
    }
}
