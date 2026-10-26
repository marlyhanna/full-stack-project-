using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Models;
using LibrarySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = UserRoles.User + "," + UserRoles.Admin)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    /// <summary>Retrieve notifications for the user/admin inbox.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Notification>), 200)]
    public async Task<IActionResult> List([FromQuery] NotificationsQuery query, CancellationToken ct)
        => Ok(await _notifications.ListAsync(query, ct));

    /// <summary>Mark a notification as read.</summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(Notification), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        => Ok(await _notifications.MarkAsReadAsync(id, ct));
}
