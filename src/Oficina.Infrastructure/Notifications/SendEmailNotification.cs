using Oficina.Application.Notifications;
using System.Net.Mail;

namespace Oficina.Infrastructure.Notifications;

public sealed class SendEmailNotification(
    IEmailTransport emailSender)
{
    private const string Subject = "Notificação da Oficina";
    private const string Body = "Esta é uma notificação enviada pela Oficina.";

    private readonly IEmailTransport _emailSender = emailSender;

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
