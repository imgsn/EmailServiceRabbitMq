using EmailService.Domain.Common;
using EmailService.Domain.Enums;

namespace EmailService.Domain.Entities;

public class EmailQueue : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? TemplateId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public string? Attachments { get; set; } // JSON array of file paths
    public string? Metadata { get; set; } // JSON for additional data
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public EmailTemplate? Template { get; set; }
}
