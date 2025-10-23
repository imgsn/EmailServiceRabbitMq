using EmailService.Application.Common.Models;
using EmailService.Application.Features.Emails.Commands;
using EmailService.Application.Features.Emails.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IMediator mediator, ILogger<EmailController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send a single email
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailResponse>> SendEmail(
        [FromBody] SendEmailRequest request,
        [FromQuery] bool sendImmediately = false)
    {
        _logger.LogInformation("Received send email request for {ToEmail}", request.ToEmail);

        var command = new SendEmailCommand
        {
            Request = request,
            SendImmediately = sendImmediately
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Send an email using a template
    /// </summary>
    [HttpPost("send-template")]
    [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailResponse>> SendTemplateEmail(
        [FromBody] SendTemplateEmailRequest request,
        [FromQuery] bool sendImmediately = false)
    {
        _logger.LogInformation("Received send template email request for template {TemplateId}", request.TemplateId);

        var command = new SendTemplateEmailCommand
        {
            Request = request,
            SendImmediately = sendImmediately
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Send bulk emails
    /// </summary>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(BulkEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkEmailResponse>> SendBulkEmail(
        [FromBody] BulkEmailRequest request,
        [FromQuery] bool sendImmediately = false)
    {
        _logger.LogInformation("Received bulk email request for {Count} emails", request.Emails.Count);

        var command = new SendBulkEmailCommand
        {
            Request = request,
            SendImmediately = sendImmediately
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Send bulk emails using a template
    /// </summary>
    [HttpPost("send-bulk-template")]
    [ProducesResponseType(typeof(BulkEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkEmailResponse>> SendBulkTemplateEmail(
        [FromBody] BulkTemplateEmailRequest request,
        [FromQuery] bool sendImmediately = false)
    {
        _logger.LogInformation("Received bulk template email request for template {TemplateId}", request.TemplateId);

        var bulkRequest = new BulkEmailRequest
        {
            Emails = request.Recipients.Select(r => new SendEmailRequest
            {
                ToEmail = r.ToEmail,
                ToName = r.ToName,
                // Template will be rendered for each recipient in the command handler
                Subject = string.Empty,
                Body = string.Empty
            }).ToList()
        };

        // We'll need to modify this to handle template emails properly
        // For now, let's send individual template emails
        var responses = new List<EmailResponse>();
        foreach (var recipient in request.Recipients)
        {
            var templateRequest = new SendTemplateEmailRequest
            {
                TemplateId = request.TemplateId,
                ToEmail = recipient.ToEmail,
                ToName = recipient.ToName,
                TemplateData = recipient.TemplateData
            };

            var command = new SendTemplateEmailCommand
            {
                Request = templateRequest,
                SendImmediately = sendImmediately
            };

            var result = await _mediator.Send(command);
            responses.Add(result);
        }

        var bulkResponse = new BulkEmailResponse
        {
            TotalEmails = responses.Count,
            SuccessCount = responses.Count(r => r.Status != "Failed"),
            FailedCount = responses.Count(r => r.Status == "Failed"),
            Results = responses
        };

        return Ok(bulkResponse);
    }

    /// <summary>
    /// Get email status by ID
    /// </summary>
    [HttpGet("{emailId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailStatus(Guid emailId)
    {
        _logger.LogInformation("Received get email status request for {EmailId}", emailId);

        var query = new GetEmailStatusQuery { EmailId = emailId };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }


    // create Bing for test
    [HttpGet("test-bing")]
    public IActionResult TestBing()
    {
        return Ok("Bing test successful");

    }
}
