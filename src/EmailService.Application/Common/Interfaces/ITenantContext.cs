using EmailService.Domain.Entities;

namespace EmailService.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    Tenant? CurrentTenant { get; }
    Task<Tenant?> GetTenantByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    void SetTenant(Guid tenantId);
    void SetTenant(Tenant tenant);
}