using System.Net.Mail;

namespace Oficina.Application.Notifications.UseCases;

public class SendEmailNotificationUseCase(
    INotificationEmailSender emailSender)
{
    private const string Subject = "Notificação da Oficina";
    private const string Body = "Esta é uma notificação enviada pela Oficina.";

    private readonly INotificationEmailSender _emailSender = emailSender;

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
}
