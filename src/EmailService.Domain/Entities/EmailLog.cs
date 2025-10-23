using EmailService.Domain.Common;
using EmailService.Domain.Enums;

namespace EmailService.Domain.Entities;

public class EmailLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? QueueId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SmtpResponse { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public EmailQueue? Queue { get; set; }
}
