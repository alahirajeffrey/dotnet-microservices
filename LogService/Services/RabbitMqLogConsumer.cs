using System.Text;
using System.Text.Json;
using LogService.Models;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogService.Services;

public class RabbitMqLogConsumer : BackgroundService
{
    private readonly IMongoDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqLogConsumer> _logger;

    public RabbitMqLogConsumer(
        IMongoDatabase database,
        IConfiguration configuration,
        ILogger<RabbitMqLogConsumer> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["RabbitMQ:HostName"] ?? "localhost";
        var user = _configuration["RabbitMQ:UserName"] ?? "rabbitmq";
        var password = _configuration["RabbitMQ:Password"] ?? "password";

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var connectionDispose = connection;

        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await using var channelDispose = channel;

        await channel.QueueDeclareAsync(
            queue: "event-logs",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var collection = _database.GetCollection<LogEntry>("event_logs");
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var payload = Encoding.UTF8.GetString(body);
                var logEntry = JsonSerializer.Deserialize<LogEntry>(payload);

                if (logEntry is not null)
                {
                    await collection.InsertOneAsync(logEntry, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store log event");
            }
            finally
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "event-logs",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
