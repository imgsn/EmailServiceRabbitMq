using EmailService.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EmailService.Infrastructure.Messaging;

public class RabbitMqMessageBroker : IMessageBroker, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqMessageBroker> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "email-queue";

    public RabbitMqMessageBroker(IConfiguration configuration, ILogger<RabbitMqMessageBroker> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public Task PublishEmailAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.DeliveryMode = 2;

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: QueueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message published to RabbitMQ: {Message}", json);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to RabbitMQ");
            throw;
        }
    }

    public async Task PublishBulkEmailsAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class
    {
        foreach (var message in messages)
        {
            await PublishEmailAsync(message, cancellationToken);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        _logger.LogInformation("RabbitMQ connection disposed");
    }
}
