using EmailService.Domain.Common;

namespace EmailService.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ContactEmail { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool? SmtpEnableSsl { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public int DailyEmailLimit { get; set; } = 1000;
    public int HourlyEmailLimit { get; set; } = 100;
    
    // Navigation properties
    public ICollection<EmailTemplate> EmailTemplates { get; set; } = new List<EmailTemplate>();
    public ICollection<EmailQueue> EmailQueues { get; set; } = new List<EmailQueue>();
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}
