using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LibrarySystem.Api.Messaging;

public interface IBorrowRequestPublisher
{
    void Publish(BorrowRequestMessage message);
}

/// <summary>
/// Publishes borrow request events to RabbitMQ. Connection is opened lazily
/// and reused; each Publish call opens a short-lived channel, which is the
/// recommended RabbitMQ.Client pattern (connections are expensive, channels
/// are cheap).
/// </summary>
public class BorrowRequestPublisher : IBorrowRequestPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<BorrowRequestPublisher> _logger;
    private readonly Lazy<IConnection> _connection;

    public BorrowRequestPublisher(IOptions<RabbitMqSettings> settings, ILogger<BorrowRequestPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _connection = new Lazy<IConnection>(CreateConnection);
    }

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };
        return factory.CreateConnection("library-system-api");
    }

    public void Publish(BorrowRequestMessage message)
    {
        try
        {
            using var channel = _connection.Value.CreateModel();
            channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(exchange: string.Empty, routingKey: _settings.QueueName, basicProperties: properties, body: body);
            _logger.LogInformation("Published borrow request {RequestId} to queue {Queue}", message.RequestId, _settings.QueueName);
        }
        catch (Exception ex)
        {
            // A RabbitMQ outage shouldn't fail the borrow request itself -
            // the request is already persisted. We log loudly so it can be
            // alerted on; the admin will simply miss the real-time push and
            // see it on next poll/list instead.
            _logger.LogError(ex, "Failed to publish borrow request {RequestId} to RabbitMQ", message.RequestId);
        }
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
