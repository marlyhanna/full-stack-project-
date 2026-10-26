using System.Text;
using System.Text.Json;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Hubs;
using LibrarySystem.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LibrarySystem.Api.Messaging;

/// <summary>
/// The "background service" described in the docs: listens to the RabbitMQ
/// borrow-requests queue, and for each message creates an admin-facing
/// Notification record and pushes it to connected admins in real time via
/// SignalR.
/// </summary>
public class BorrowRequestConsumerService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<BorrowRequestConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public BorrowRequestConsumerService(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hub,
        ILogger<BorrowRequestConsumerService> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // Retry connecting so a slow-starting RabbitMQ container doesn't
        // crash the whole API on boot.
        var attempts = 0;
        while (true)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    DispatchConsumersAsync = true
                };
                _connection = factory.CreateConnection("library-system-consumer");
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
                break;
            }
            catch (Exception ex) when (++attempts < 5)
            {
                _logger.LogWarning(ex, "RabbitMQ not reachable yet, retrying ({Attempt}/5)...", attempts);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not connect to RabbitMQ after 5 attempts. Borrow request notifications will not fire until it is available.");
                return Task.CompletedTask;
            }
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null) return Task.CompletedTask;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<BorrowRequestMessage>(json);
                if (message is not null)
                {
                    await HandleMessageAsync(message);
                }
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process borrow request message; requeueing.");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(_settings.QueueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BorrowRequestMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var admins = db.Users.Where(u => u.Role == UserRoles.Admin && u.IsActive).ToList();
        var notifications = new List<Notification>();

        foreach (var admin in admins)
        {
            var notification = new Notification
            {
                RecipientUserId = admin.Id,
                RecipientRole = UserRoles.Admin,
                Type = Models.NotificationType.BorrowRequestCreated,
                Title = "New borrow request",
                Message = $"{message.UserFullName} requested to borrow \"{message.BookTitle}\" for {message.BorrowingPeriodDays} day(s).",
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();

        // Push live to every connected admin.
        await _hub.Clients.Group(NotificationHub.AdminsGroup).SendAsync("ReceiveNotification", new
        {
            type = Models.NotificationType.BorrowRequestCreated,
            requestId = message.RequestId,
            bookTitle = message.BookTitle,
            requestedBy = message.UserFullName,
            borrowingPeriodDays = message.BorrowingPeriodDays,
            createdAt = DateTime.UtcNow
        });

        _logger.LogInformation("Processed borrow request {RequestId}: notified {Count} admin(s)", message.RequestId, admins.Count);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
