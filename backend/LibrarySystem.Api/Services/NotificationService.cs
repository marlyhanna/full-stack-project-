using LibrarySystem.Api.Data;
using LibrarySystem.Api.DTOs;
using LibrarySystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.Services;

public interface INotificationService
{
    Task<PagedResult<Notification>> ListAsync(NotificationsQuery query, CancellationToken ct);
    Task<Notification> MarkAsReadAsync(Guid id, CancellationToken ct);
}

public class NotificationService : INotificationService
{
    private readonly LibraryDbContext _db;

    public NotificationService(LibraryDbContext db) => _db = db;

    public async Task<PagedResult<Notification>> ListAsync(NotificationsQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var notifications = _db.Notifications.AsNoTracking().AsQueryable();

        if (query.RecipientUserId.HasValue)
            notifications = notifications.Where(n => n.RecipientUserId == query.RecipientUserId);
        if (!string.IsNullOrWhiteSpace(query.RecipientRole))
            notifications = notifications.Where(n => n.RecipientRole == query.RecipientRole);
        if (query.IsRead.HasValue)
            notifications = notifications.Where(n => n.IsRead == query.IsRead);

        notifications = notifications.OrderByDescending(n => n.CreatedAt);

        var totalItems = await notifications.CountAsync(ct);
        var items = await notifications.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Notification>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<Notification> MarkAsReadAsync(Guid id, CancellationToken ct)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new ApiException(404, "NOT_FOUND", "Notification not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return notification;
    }
}
