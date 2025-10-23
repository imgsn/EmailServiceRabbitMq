using EmailService.Domain.Enums;

namespace EmailService.Application.Common.Models;

public class SendEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string[]? CcEmails { get; set; }
    public string[]? BccEmails { get; set; }
    public string[]? Attachments { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class SendTemplateEmailRequest
{
    public Guid TemplateId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public string[]? CcEmails { get; set; }
    public string[]? BccEmails { get; set; }
    public string[]? Attachments { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class BulkEmailRequest
{
    public List<SendEmailRequest> Emails { get; set; } = new();
}

public class BulkTemplateEmailRequest
{
    public Guid TemplateId { get; set; }
    public List<BulkEmailRecipient> Recipients { get; set; } = new();
}

public class BulkEmailRecipient
{
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
}

public class EmailResponse
{
    public Guid EmailId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}

public class BulkEmailResponse
{
    public int TotalEmails { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<EmailResponse> Results { get; set; } = new();
}
