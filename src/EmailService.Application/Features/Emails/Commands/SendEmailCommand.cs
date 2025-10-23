using EmailService.Application.Common.Interfaces;
using EmailService.Application.Common.Models;
using EmailService.Domain.Entities;
using EmailService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EmailService.Application.Features.Emails.Commands;

public class SendEmailCommand : IRequest<EmailResponse>
{
    public SendEmailRequest Request { get; set; } = null!;
    public bool SendImmediately { get; set; }
}

public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, EmailResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IEmailSender _emailSender;
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IEmailSender emailSender,
        IMessageBroker messageBroker,
        ILogger<SendEmailCommandHandler> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _emailSender = emailSender;
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task<EmailResponse> Handle(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        _logger.LogInformation("Processing email for tenant {TenantId} to {ToEmail}", tenantId, request.ToEmail);

        // Create email queue entry
        var emailQueue = new EmailQueue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ToEmail = request.ToEmail,
            ToName = request.ToName,
            CcEmails = request.CcEmails != null ? string.Join(";", request.CcEmails) : null,
            BccEmails = request.BccEmails != null ? string.Join(";", request.BccEmails) : null,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            Priority = request.Priority,
            Status = EmailStatus.Pending,
            ScheduledAt = request.ScheduledAt,
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailQueues.Add(emailQueue);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email queued with ID {EmailId} for tenant {TenantId}", emailQueue.Id, tenantId);

        // Send immediately if requested
        if (command.SendImmediately)
        {
            try
            {
                _logger.LogInformation("Sending email immediately for queue ID {EmailId}", emailQueue.Id);
                
                var sent = await _emailSender.SendEmailAsync(
                    emailQueue.ToEmail,
                    emailQueue.Subject,
                    emailQueue.Body,
                    emailQueue.IsHtml,
                    emailQueue.ToName,
                    emailQueue.CcEmails?.Split(';'),
                    emailQueue.BccEmails?.Split(';'),
                    null,
                    cancellationToken);

                emailQueue.Status = sent ? EmailStatus.Sent : EmailStatus.Failed;
                emailQueue.SentAt = sent ? DateTime.UtcNow : null;
                emailQueue.ErrorMessage = sent ? null : "Failed to send email";
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Email sent immediately with status {Status} for queue ID {EmailId}", 
                    emailQueue.Status, emailQueue.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email immediately for queue ID {EmailId}", emailQueue.Id);
                emailQueue.Status = EmailStatus.Failed;
                emailQueue.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // Publish to message broker for worker to process
            await _messageBroker.PublishEmailAsync(new { EmailQueueId = emailQueue.Id }, cancellationToken);
            _logger.LogInformation("Email published to message broker for queue ID {EmailId}", emailQueue.Id);
        }

        return new EmailResponse
        {
            EmailId = emailQueue.Id,
            Status = emailQueue.Status.ToString(),
            Message = "Email queued successfully",
            QueuedAt = emailQueue.CreatedAt
        };
    }
}
