namespace EmailService.Domain.Enums;

public enum EmailStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}

public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
