using Microsoft.Extensions.Options;
using Oficina.Application.Notifications;
using System.Net;
using System.Net.Mail;

namespace Oficina.Infrastructure.Notifications;

public sealed class SmtpNotificationEmailSender : INotificationEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpNotificationEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.From))
        {
            throw new InvalidOperationException("SMTP settings are incomplete.");
        }

        using var message = new MailMessage(_options.From, recipient, subject, body)
        {
            IsBodyHtml = false
        };
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
