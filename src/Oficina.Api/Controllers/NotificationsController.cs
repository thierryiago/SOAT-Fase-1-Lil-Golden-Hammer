using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Notifications;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpPost("email", Name = "SendEmailNotification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmail(
        SendEmailNotificationRequest request,
        CancellationToken cancellationToken)
    {
        await _notifications.SendEmailAsync(request, cancellationToken);
        return NoContent();
    }
}
