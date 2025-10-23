using EmailService.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EmailService.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IConfiguration configuration,
        ITenantContext tenantContext,
        ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        string? toName = null,
        string[]? ccEmails = null,
        string[]? bccEmails = null,
        string[]? attachmentPaths = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = _tenantContext.CurrentTenant;
            if (tenant == null)
            {
                _logger.LogError("No tenant context available for sending email");
                return false;
            }

            _logger.LogInformation("Sending email to {ToEmail} for tenant {TenantId}", toEmail, tenant.Id);

            var message = new MimeMessage();

            // From address
            var fromEmail = tenant.FromEmail ?? _configuration["EmailSettings:FromEmail"];
            var fromName = tenant.FromName ?? _configuration["EmailSettings:FromName"];
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            // To address
            message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));

            // CC addresses
            if (ccEmails != null)
            {
                foreach (var cc in ccEmails)
                {
                    message.Cc.Add(MailboxAddress.Parse(cc));
                }
            }

            // BCC addresses
            if (bccEmails != null)
            {
                foreach (var bcc in bccEmails)
                {
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
                }
            }

            message.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }

            // Attachments
            if (attachmentPaths != null)
            {
                foreach (var path in attachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        builder.Attachments.Add(path);
                    }
                }
            }

            message.Body = builder.ToMessageBody();

            // SMTP configuration
            var smtpHost = tenant.SmtpHost ?? _configuration["EmailSettings:SmtpHost"];
            var smtpPort = tenant.SmtpPort ?? int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = tenant.SmtpUsername ?? _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = tenant.SmtpPassword ?? _configuration["EmailSettings:SmtpPassword"];
            var enableSsl = tenant.SmtpEnableSsl ?? bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

            using var client = new SmtpClient();
            
            await client.ConnectAsync(smtpHost, smtpPort, 
                enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                await client.AuthenticateAsync(smtpUsername, smtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            return false;
        }
    }
}
