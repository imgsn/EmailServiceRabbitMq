using EmailService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailService.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailQueue> EmailQueues { get; }
    DbSet<EmailLog> EmailLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
