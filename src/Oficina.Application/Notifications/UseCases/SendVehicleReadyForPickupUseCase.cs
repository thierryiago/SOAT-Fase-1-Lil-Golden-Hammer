using System.Text;

namespace Oficina.Application.Notifications.UseCases;

public class SendVehicleReadyForPickupUseCase(
    INotificationEmailSender emailSender)
{
    private readonly INotificationEmailSender _emailSender = emailSender;

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
}
