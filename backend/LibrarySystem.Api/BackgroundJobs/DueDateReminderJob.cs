using LibrarySystem.Api.Data;
using LibrarySystem.Api.Hubs;
using LibrarySystem.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.BackgroundJobs;

public class DueDateReminderSettings
{
    public int IntervalMinutes { get; set; } = 60;
    public int DaysBeforeDueToRemind { get; set; } = 2;
}

/// <summary>
/// The "5. Background Job" from the docs: runs periodically, identifies
/// books close to the end of their borrowing period, and sends reminder
/// notifications to both the borrower and admins. Avoids duplicate
/// reminders by checking whether one was already sent for a given request.
/// </summary>
public class DueDateReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly DueDateReminderSettings _settings;
    private readonly ILogger<DueDateReminderJob> _logger;

    public DueDateReminderJob(
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hub,
        Microsoft.Extensions.Options.IOptions<DueDateReminderSettings> settings,
        ILogger<DueDateReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDueDatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Due-date reminder job iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.IntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckDueDatesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var reminderThreshold = DateTime.UtcNow.AddDays(_settings.DaysBeforeDueToRemind);

        var dueSoon = await db.BorrowingRequests
            .Include(r => r.User)
            .Where(r => r.Status == RequestStatus.Approved
                        && r.DueDate != null
                        && r.DueDate <= reminderThreshold
                        && r.DueDate >= DateTime.UtcNow)
            .ToListAsync(ct);

        if (dueSoon.Count == 0) return;

        var admins = await db.Users.Where(u => u.Role == UserRoles.Admin && u.IsActive).ToListAsync(ct);

        foreach (var request in dueSoon)
        {
            // Skip if we've already sent a reminder for this request.
            var alreadyReminded = await db.Notifications.AnyAsync(n =>
                n.Type == Models.NotificationType.BorrowDueReminder &&
                n.Message.Contains(request.Id.ToString()), ct);
            if (alreadyReminded) continue;

            var title = "Borrowing period ending soon";
            var message = $"\"{request.BookTitle}\" is due on {request.DueDate:yyyy-MM-dd}. [ref:{request.Id}]";

            var notifications = new List<Notification>
            {
                new()
                {
                    RecipientUserId = request.UserId,
                    RecipientRole = UserRoles.User,
                    Type = Models.NotificationType.BorrowDueReminder,
                    Title = title,
                    Message = message
                }
            };
            notifications.AddRange(admins.Select(a => new Notification
            {
                RecipientUserId = a.Id,
                RecipientRole = UserRoles.Admin,
                Type = Models.NotificationType.BorrowDueReminder,
                Title = title,
                Message = message
            }));

            db.Notifications.AddRange(notifications);

            await _hub.Clients.Group($"user:{request.UserId}").SendAsync("ReceiveNotification", new
            {
                type = Models.NotificationType.BorrowDueReminder,
                requestId = request.Id,
                bookTitle = request.BookTitle,
                dueDate = request.DueDate
            }, ct);

            await _hub.Clients.Group(NotificationHub.AdminsGroup).SendAsync("ReceiveNotification", new
            {
                type = Models.NotificationType.BorrowDueReminder,
                requestId = request.Id,
                bookTitle = request.BookTitle,
                dueDate = request.DueDate
            }, ct);
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Due-date reminder job sent reminders for {Count} request(s)", dueSoon.Count);
    }
}
