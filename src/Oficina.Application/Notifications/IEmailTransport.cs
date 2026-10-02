namespace Oficina.Application.Notifications;

public interface IEmailTransport
{
    Task SendAsync(
        string recipient,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken);
}
