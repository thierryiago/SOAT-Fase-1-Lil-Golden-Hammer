using System.ComponentModel.DataAnnotations;

namespace Oficina.Application.Notifications;

public sealed record SendEmailNotificationRequest([Required, EmailAddress] string Email);
