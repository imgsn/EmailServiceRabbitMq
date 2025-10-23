using EmailService.Application.Common.Interfaces;
using EmailService.Domain.Entities;
using EmailService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EmailService.Worker;

public class EmailWorker : BackgroundService
{
    private readonly ILogger<EmailWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "email-queue";

    public EmailWorker(
        ILogger<EmailWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Worker Service is starting");
        InitializeRabbitMq();
        return base.StartAsync(cancellationToken);
    }

    private void InitializeRabbitMq()
    {
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

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ connection established for Email Worker");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Worker Service is executing");

        stoppingToken.Register(() => _logger.LogInformation("Email Worker Service is stopping"));

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received message from queue: {Message}", message);

            try
            {
                var emailMessage = JsonSerializer.Deserialize<EmailQueueMessage>(message);
                if (emailMessage?.EmailQueueId != null)
                {
                    await ProcessEmailAsync(emailMessage.EmailQueueId, stoppingToken);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Message processed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
                // Reject and requeue
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Email Worker is listening for messages");

        // Also process scheduled emails periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled emails");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessEmailAsync(Guid emailQueueId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        _logger.LogInformation("Processing email queue ID: {EmailQueueId}", emailQueueId);

        var emailQueue = await context.EmailQueues
            .Include(e => e.Tenant)
            .FirstOrDefaultAsync(e => e.Id == emailQueueId, cancellationToken);

        if (emailQueue == null)
        {
            _logger.LogWarning("Email queue {EmailQueueId} not found", emailQueueId);
            return;
        }

        if (emailQueue.Status == EmailStatus.Sent)
        {
            _logger.LogInformation("Email {EmailQueueId} already sent", emailQueueId);
            return;
        }

        // Check if scheduled for future
        if (emailQueue.ScheduledAt.HasValue && emailQueue.ScheduledAt.Value > DateTime.UtcNow)
        {
            _logger.LogInformation("Email {EmailQueueId} scheduled for {ScheduledAt}",
                emailQueueId, emailQueue.ScheduledAt);
            return;
        }

        // Set tenant context with full tenant object
        tenantContext.SetTenant(emailQueue.Tenant);

        // Update status to processing
        emailQueue.Status = EmailStatus.Processing;
        await context.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Sending email to {ToEmail}", emailQueue.ToEmail);

            var sent = await emailSender.SendEmailAsync(
                emailQueue.ToEmail,
                emailQueue.Subject,
                emailQueue.Body,
                emailQueue.IsHtml,
                emailQueue.ToName,
                emailQueue.CcEmails?.Split(';'),
                emailQueue.BccEmails?.Split(';'),
                null,
                cancellationToken);

            stopwatch.Stop();

            if (sent)
            {
                emailQueue.Status = EmailStatus.Sent;
                emailQueue.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Email {EmailQueueId} sent successfully in {ElapsedMs}ms",
                    emailQueueId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                emailQueue.RetryCount++;
                if (emailQueue.RetryCount >= emailQueue.MaxRetries)
                {
                    emailQueue.Status = EmailStatus.Failed;
                    emailQueue.ErrorMessage = "Max retries exceeded";
                    _logger.LogWarning("Email {EmailQueueId} failed after {RetryCount} retries",
                        emailQueueId, emailQueue.RetryCount);
                }
                else
                {
                    emailQueue.Status = EmailStatus.Pending;
                    _logger.LogInformation("Email {EmailQueueId} will be retried (attempt {RetryCount})",
                        emailQueueId, emailQueue.RetryCount + 1);
                }
            }

            // Create log entry
            var log = new EmailLog
            {
                Id = Guid.NewGuid(),
                TenantId = emailQueue.TenantId,
                QueueId = emailQueue.Id,
                ToEmail = emailQueue.ToEmail,
                Subject = emailQueue.Subject,
                Status = emailQueue.Status,
                SentAt = emailQueue.SentAt,
                ErrorMessage = emailQueue.ErrorMessage,
                RetryCount = emailQueue.RetryCount,
                ProcessingTime = stopwatch.Elapsed,
                CreatedAt = DateTime.UtcNow
            };

            context.EmailLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error sending email {EmailQueueId}", emailQueueId);

            emailQueue.RetryCount++;
            emailQueue.ErrorMessage = ex.Message;

            if (emailQueue.RetryCount >= emailQueue.MaxRetries)
            {
                emailQueue.Status = EmailStatus.Failed;
            }
            else
            {
                emailQueue.Status = EmailStatus.Pending;
            }

            // Create log entry
            var log = new EmailLog
            {
                Id = Guid.NewGuid(),
                TenantId = emailQueue.TenantId,
                QueueId = emailQueue.Id,
                ToEmail = emailQueue.ToEmail,
                Subject = emailQueue.Subject,
                Status = emailQueue.Status,
                ErrorMessage = ex.Message,
                RetryCount = emailQueue.RetryCount,
                ProcessingTime = stopwatch.Elapsed,
                CreatedAt = DateTime.UtcNow
            };

            context.EmailLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var scheduledEmails = await context.EmailQueues
            .Where(e => e.Status == EmailStatus.Pending &&
                       e.ScheduledAt.HasValue &&
                       e.ScheduledAt.Value <= DateTime.UtcNow)
            .Take(100)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processing {Count} scheduled emails", scheduledEmails.Count);

        foreach (var email in scheduledEmails)
        {
            await ProcessEmailAsync(email.Id, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Worker Service is stopping");
        _channel?.Close();
        _connection?.Close();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    private class EmailQueueMessage
    {
        public Guid EmailQueueId { get; set; }
    }
}