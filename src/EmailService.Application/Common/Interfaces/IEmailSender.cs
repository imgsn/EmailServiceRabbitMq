namespace EmailService.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        string? toName = null,
        string[]? ccEmails = null,
        string[]? bccEmails = null,
        string[]? attachmentPaths = null,
        CancellationToken cancellationToken = default);
}
