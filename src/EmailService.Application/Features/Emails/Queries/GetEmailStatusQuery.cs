using EmailService.Application.Common.Interfaces;
using EmailService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmailService.Application.Features.Emails.Queries;

public class GetEmailStatusQuery : IRequest<EmailQueue?>
{
    public Guid EmailId { get; set; }
}

public class GetEmailStatusQueryHandler : IRequestHandler<GetEmailStatusQuery, EmailQueue?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetEmailStatusQueryHandler> _logger;

    public GetEmailStatusQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<GetEmailStatusQueryHandler> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<EmailQueue?> Handle(GetEmailStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        _logger.LogInformation("Getting email status for {EmailId}, tenant {TenantId}", 
            request.EmailId, tenantId);

        return await _context.EmailQueues
            .Include(e => e.Template)
            .FirstOrDefaultAsync(e => e.Id == request.EmailId && e.TenantId == tenantId, cancellationToken);
    }
}
