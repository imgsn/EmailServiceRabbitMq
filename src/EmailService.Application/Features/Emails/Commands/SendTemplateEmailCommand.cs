using EmailService.Application.Common.Interfaces;
using EmailService.Application.Common.Models;
using EmailService.Domain.Entities;
using EmailService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scriban;
using System.Text.Json;

namespace EmailService.Application.Features.Emails.Commands;

public class SendTemplateEmailCommand : IRequest<EmailResponse>
{
    public SendTemplateEmailRequest Request { get; set; } = null!;
    public bool SendImmediately { get; set; }
}

public class SendTemplateEmailCommandHandler : IRequestHandler<SendTemplateEmailCommand, EmailResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IEmailSender _emailSender;
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<SendTemplateEmailCommandHandler> _logger;

    public SendTemplateEmailCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IEmailSender emailSender,
        IMessageBroker messageBroker,
        ILogger<SendTemplateEmailCommandHandler> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _emailSender = emailSender;
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task<EmailResponse> Handle(SendTemplateEmailCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        _logger.LogInformation("Processing template email for tenant {TenantId}, template {TemplateId}", 
            tenantId, request.TemplateId);

        // Get template
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == tenantId && t.IsActive, 
                cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("Template {TemplateId} not found for tenant {TenantId}", 
                request.TemplateId, tenantId);
            throw new InvalidOperationException("Email template not found");
        }

        // Render template with Scriban
        var subjectTemplate = Template.Parse(template.Subject);
        var bodyTemplate = Template.Parse(template.Body);

        var subject = await subjectTemplate.RenderAsync(request.TemplateData);
        var body = await bodyTemplate.RenderAsync(request.TemplateData);

        _logger.LogInformation("Template rendered for template {TemplateId}", request.TemplateId);

        // Create email queue entry
        var emailQueue = new EmailQueue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = template.Id,
            ToEmail = request.ToEmail,
            ToName = request.ToName,
            CcEmails = request.CcEmails != null ? string.Join(";", request.CcEmails) : null,
            BccEmails = request.BccEmails != null ? string.Join(";", request.BccEmails) : null,
            Subject = subject,
            Body = body,
            IsHtml = template.IsHtml,
            Priority = request.Priority,
            Status = EmailStatus.Pending,
            ScheduledAt = request.ScheduledAt,
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailQueues.Add(emailQueue);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Template email queued with ID {EmailId}", emailQueue.Id);

        // Send immediately if requested
        if (command.SendImmediately)
        {
            try
            {
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
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Template email sent immediately with status {Status}", emailQueue.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email immediately");
                emailQueue.Status = EmailStatus.Failed;
                emailQueue.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            await _messageBroker.PublishEmailAsync(new { EmailQueueId = emailQueue.Id }, cancellationToken);
            _logger.LogInformation("Template email published to message broker");
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
