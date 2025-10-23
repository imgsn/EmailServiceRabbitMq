using EmailService.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check (no auth required)
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "Email Service API"
        });
    }

    /// <summary>
    /// Detailed health check with database connection test (requires auth)
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        try
        {
            // Test database connection
            var tenantsCount = await _context.Tenants.CountAsync();
            var pendingEmails = await _context.EmailQueues
                .CountAsync(e => e.Status == Domain.Enums.EmailStatus.Pending);

            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                service = "Email Service API",
                database = new
                {
                    connected = true,
                    tenantsCount,
                    pendingEmails
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test endpoint to verify middleware (requires auth)
    /// </summary>
    [HttpGet("auth-test")]
    public IActionResult TestAuth([FromServices] ITenantContext tenantContext)
    {
        if (tenantContext.CurrentTenant == null)
        {
            return Unauthorized(new { message = "Tenant not authenticated" });
        }

        return Ok(new
        {
            message = "Authentication successful!",
            tenantId = tenantContext.TenantId,
            tenantName = tenantContext.CurrentTenant.Name,
            timestamp = DateTime.UtcNow
        });
    }
}
