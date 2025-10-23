using EmailService.Application.Common.Interfaces;
using EmailService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailService.API.Middleware;

public class TenantAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthenticationMiddleware> _logger;

    public TenantAuthenticationMiddleware(RequestDelegate next, ILogger<TenantAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip authentication for these endpoints
        if (path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path.Contains("/swagger/") ||
            path == "/" ||
            path == "/api/health")
        {
            _logger.LogDebug("Skipping auth for public path: {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        _logger.LogInformation("Authenticating request for path: {Path}", context.Request.Path);

        // Get API key from header - properly convert StringValues to string
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues))
        {
            _logger.LogWarning("Request without API key from {IpAddress} to {Path}",
                context.Connection.RemoteIpAddress, context.Request.Path);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "API Key is required",
                message = "Please provide X-API-Key header with your tenant API key",
                path = context.Request.Path.Value
            });
            return;
        }

        var apiKey = apiKeyValues.ToString(); // Convert StringValues to string

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Empty API key from {IpAddress} to {Path}",
                context.Connection.RemoteIpAddress, context.Request.Path);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "API Key is required",
                message = "API key cannot be empty"
            });
            return;
        }

        // Validate API key and set tenant context
        try
        {
            _logger.LogDebug("Validating API key: {ApiKeyPrefix}...",
                apiKey.Length > 10 ? apiKey.Substring(0, 10) + "..." : apiKey);

            // Lookup tenant from database directly
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var tenant = await dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.IsActive && !t.IsDeleted);

            if (tenant == null)
            {
                _logger.LogWarning("Invalid API key attempted from {IpAddress}",
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid API Key",
                    message = "The provided API key is not valid or tenant is inactive"
                });
                return;
            }

            // Set tenant in the scoped tenant context
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(tenant);

            // Store tenant info in HttpContext for easy access
            context.Items["TenantId"] = tenant.Id;
            context.Items["TenantName"] = tenant.Name;
            context.Items["Tenant"] = tenant;

            _logger.LogInformation("Tenant '{TenantName}' (ID: {TenantId}) authenticated successfully",
                tenant.Name, tenant.Id);

            // Continue to next middleware/controller
            await _next(context);

            _logger.LogDebug("Request completed successfully for path: {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant authentication for path {Path}", context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal server error during authentication",
                message = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
    }
}