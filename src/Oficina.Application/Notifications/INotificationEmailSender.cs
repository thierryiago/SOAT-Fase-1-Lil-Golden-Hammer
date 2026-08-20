namespace Oficina.Application.Notifications;

public interface INotificationEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
