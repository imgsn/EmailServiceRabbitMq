using EmailService.Application.Common.Interfaces;
using EmailService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EmailService.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private readonly ILogger<TenantContext> _logger;
    private Guid? _tenantId;
    private Tenant? _currentTenant;

    public TenantContext(ILogger<TenantContext> logger)
    {
        _logger = logger;
    }

    public Guid? TenantId => _tenantId;
    public Tenant? CurrentTenant => _currentTenant;

    public Task<Tenant?> GetTenantByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // This method should not be used directly anymore
        // Tenant lookup is now done in the middleware
        _logger.LogWarning("GetTenantByApiKeyAsync called - this should be done in middleware");
        return Task.FromResult(_currentTenant);
    }

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        _logger.LogInformation("Tenant context set to {TenantId}", tenantId);
    }

    public void SetTenant(Tenant tenant)
    {
        _tenantId = tenant.Id;
        _currentTenant = tenant;
        _logger.LogInformation("Tenant context set to {TenantId} ({TenantName})", tenant.Id, tenant.Name);
    }
}