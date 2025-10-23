using EmailService.Application.Common.Interfaces;
using EmailService.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EmailService.Application.Features.Emails.Commands;

public class SendBulkEmailCommand : IRequest<BulkEmailResponse>
{
    public BulkEmailRequest Request { get; set; } = null!;
    public bool SendImmediately { get; set; }
}

public class SendBulkEmailCommandHandler : IRequestHandler<SendBulkEmailCommand, BulkEmailResponse>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SendBulkEmailCommandHandler> _logger;

    public SendBulkEmailCommandHandler(
        IMediator mediator,
        ILogger<SendBulkEmailCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BulkEmailResponse> Handle(SendBulkEmailCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing bulk email with {Count} recipients", command.Request.Emails.Count);

        var response = new BulkEmailResponse
        {
            TotalEmails = command.Request.Emails.Count
        };

        foreach (var email in command.Request.Emails)
        {
            try
            {
                var result = await _mediator.Send(new SendEmailCommand
                {
                    Request = email,
                    SendImmediately = command.SendImmediately
                }, cancellationToken);

                response.Results.Add(result);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk email to {ToEmail}", email.ToEmail);
                response.Results.Add(new EmailResponse
                {
                    EmailId = Guid.Empty,
                    Status = "Failed",
                    Message = ex.Message,
                    QueuedAt = DateTime.UtcNow
                });
                response.FailedCount++;
            }
        }

        _logger.LogInformation("Bulk email completed: {Success} success, {Failed} failed", 
            response.SuccessCount, response.FailedCount);

        return response;
    }
}
