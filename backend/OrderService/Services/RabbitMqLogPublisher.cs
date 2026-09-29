using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService.Services;

public class RabbitMqLogPublisher
{
    private readonly ConnectionFactory _connectionFactory;

    public RabbitMqLogPublisher(IConfiguration configuration)
    {
        _connectionFactory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = configuration["RabbitMQ:UserName"] ?? "rabbitmq",
            Password = configuration["RabbitMQ:Password"] ?? "password"
        };
    }

    public async Task PublishAsync(string serviceName, string eventName, string message, object? details = null, string? userId = null)
    {
        var connection = await _connectionFactory.CreateConnectionAsync();
        await using var _ = connection;

        var channel = await connection.CreateChannelAsync();
        await using var channelDispose = channel;

        await channel.QueueDeclareAsync(
            queue: "event-logs",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var payload = new
        {
            Id = Guid.NewGuid().ToString("N"),
            ServiceName = serviceName,
            EventName = eventName,
            Message = message,
            Level = "Information",
            Details = details is null ? null : JsonSerializer.Serialize(details),
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "event-logs",
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}
